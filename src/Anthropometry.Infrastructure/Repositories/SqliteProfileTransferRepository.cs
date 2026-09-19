using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Profiles;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;
using Anthropometry.Infrastructure.Persistence.Sqlite;

namespace Anthropometry.Infrastructure.Repositories;

public sealed class SqliteProfileTransferRepository : IProfileTransferRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteProfileTransferRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Task<ProfileTransferSnapshot?> GetSnapshotAsync(ProfileId profileId, CancellationToken cancellationToken)
        => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = _connectionFactory.Create();
            var profileRow = connection.FindWithQuery<ProfileRow>(
                "SELECT Id, Name, Gender, HeightCm, AgeYears, ActivityLevel, CreatedAtUtc, UpdatedAtUtc FROM Profiles WHERE Id = ?",
                profileId.ToString());
            if (profileRow is null)
            {
                return null;
            }

            var profile = Map(profileRow);
            var measurementRows = connection.Query<MeasurementRow>(
                "SELECT Id, ProfileId, MeasuredAtUtc, MeasurementType, WeightKg, HeightCm, NeckCm, AbdomenCm, HipCm, Gender, AgeYears, ActivityLevel FROM Measurements WHERE ProfileId = ? ORDER BY MeasuredAtUtc",
                profileId.ToString());
            var measurements = measurementRows.Select(Map).ToArray();
            var results = connection.Query<CalculationResultRow>(
                    "SELECT Id, MeasurementId, CalculationType, FormulaId, FormulaVersion, Value, Unit, CalculatedAtUtc FROM CalculationResults WHERE MeasurementId IN (SELECT Id FROM Measurements WHERE ProfileId = ?) ORDER BY CalculatedAtUtc",
                    profileId.ToString())
                .Select(Map)
                .ToArray();

            return new ProfileTransferSnapshot(profile, measurements, results);
        }, cancellationToken);

    public Task ImportAsync(
        Profile profile,
        IReadOnlyList<Measurement> measurements,
        IReadOnlyList<CalculationResult> results,
        CancellationToken cancellationToken)
        => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = _connectionFactory.Create();
            connection.RunInTransaction(() =>
            {
                connection.Execute(
                    "INSERT INTO Profiles (Id, Name, Gender, HeightCm, AgeYears, ActivityLevel, CreatedAtUtc, UpdatedAtUtc) VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                    profile.Id.ToString(),
                    profile.Name,
                    (int)profile.Gender,
                    profile.Settings?.HeightCm,
                    profile.Settings?.AgeYears,
                    profile.Settings is null ? null : (int)profile.Settings.ActivityLevel,
                    SqliteValueConverter.ToUtcString(profile.CreatedAtUtc),
                    SqliteValueConverter.ToUtcString(profile.UpdatedAtUtc));

                foreach (var measurement in measurements)
                {
                    connection.Execute(
                        "INSERT INTO Measurements (Id, ProfileId, MeasuredAtUtc, MeasurementType, WeightKg, HeightCm, NeckCm, AbdomenCm, HipCm, Gender, AgeYears, ActivityLevel) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                        measurement.Id.ToString(),
                        measurement.ProfileId.ToString(),
                        SqliteValueConverter.ToUtcString(measurement.MeasuredAtUtc),
                        (int)measurement.Type,
                        measurement.WeightKg,
                        measurement.HeightCm,
                        measurement.NeckCm,
                        measurement.AbdomenCm,
                        measurement.HipCm,
                        (int)measurement.Gender,
                        measurement.AgeYears,
                        (int)measurement.ActivityLevel);
                }

                foreach (var result in results)
                {
                    connection.Execute(
                        "INSERT INTO CalculationResults (Id, MeasurementId, CalculationType, FormulaId, FormulaVersion, Value, Unit, CalculatedAtUtc) VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                        result.Id.ToString(),
                        result.MeasurementId.ToString(),
                        (int)result.CalculationType,
                        result.FormulaId,
                        result.FormulaVersion,
                        result.Value,
                        result.Unit,
                        SqliteValueConverter.ToUtcString(result.CalculatedAtUtc));
                }
            });
        }, cancellationToken);

    private static Profile Map(ProfileRow row)
    {
        ProfileSettings? settings;
        if (row.HeightCm.HasValue && row.AgeYears.HasValue && row.ActivityLevel.HasValue)
        {
            var settingsResult = ProfileSettings.Create(row.HeightCm.Value, row.AgeYears.Value, (ActivityLevel)row.ActivityLevel.Value);
            settings = settingsResult.IsSuccess
                ? settingsResult.Value
                : throw new InvalidDataException(settingsResult.Error!.Code);
        }
        else if (row.HeightCm is null && row.AgeYears is null && row.ActivityLevel is null)
        {
            settings = null;
        }
        else
        {
            throw new InvalidDataException("profile.settings.incomplete");
        }

        var result = Profile.Rehydrate(
            new ProfileId(Guid.Parse(row.Id)),
            row.Name,
            settings,
            SqliteValueConverter.ToUtcDateTimeOffset(row.CreatedAtUtc),
            SqliteValueConverter.ToUtcDateTimeOffset(row.UpdatedAtUtc),
            (ProfileGender)row.Gender);
        return result.IsSuccess ? result.Value : throw new InvalidDataException(result.Error!.Code);
    }

    private static Measurement Map(MeasurementRow row)
    {
        var input = new MeasurementInput(
            (MeasurementType)row.MeasurementType,
            row.WeightKg,
            row.HeightCm,
            row.NeckCm,
            row.AbdomenCm,
            row.AgeYears,
            (ActivityLevel)row.ActivityLevel,
            SqliteValueConverter.ToUtcDateTimeOffset(row.MeasuredAtUtc),
            row.HipCm,
            (ProfileGender)row.Gender);
        var result = Measurement.Rehydrate(
            new MeasurementId(Guid.Parse(row.Id)),
            new ProfileId(Guid.Parse(row.ProfileId)),
            input);
        return result.IsSuccess ? result.Value : throw new InvalidDataException(result.Error!.Code);
    }

    private static CalculationResult Map(CalculationResultRow row)
    {
        var result = CalculationResult.Rehydrate(
            new CalculationResultId(Guid.Parse(row.Id)),
            new MeasurementId(Guid.Parse(row.MeasurementId)),
            (CalculationType)row.CalculationType,
            new CalculationResultValue(row.Value, row.Unit, row.FormulaId, row.FormulaVersion),
            SqliteValueConverter.ToUtcDateTimeOffset(row.CalculatedAtUtc));
        return result.IsSuccess ? result.Value : throw new InvalidDataException(result.Error!.Code);
    }

    private sealed class ProfileRow
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Gender { get; set; }
        public decimal? HeightCm { get; set; }
        public int? AgeYears { get; set; }
        public int? ActivityLevel { get; set; }
        public string CreatedAtUtc { get; set; } = string.Empty;
        public string UpdatedAtUtc { get; set; } = string.Empty;
    }

    private sealed class MeasurementRow
    {
        public string Id { get; set; } = string.Empty;
        public string ProfileId { get; set; } = string.Empty;
        public string MeasuredAtUtc { get; set; } = string.Empty;
        public int MeasurementType { get; set; }
        public decimal WeightKg { get; set; }
        public decimal HeightCm { get; set; }
        public decimal? NeckCm { get; set; }
        public decimal? AbdomenCm { get; set; }
        public decimal? HipCm { get; set; }
        public int Gender { get; set; } = (int)ProfileGender.Male;
        public int AgeYears { get; set; }
        public int ActivityLevel { get; set; }
    }

    private sealed class CalculationResultRow
    {
        public string Id { get; set; } = string.Empty;
        public string MeasurementId { get; set; } = string.Empty;
        public int CalculationType { get; set; }
        public string FormulaId { get; set; } = string.Empty;
        public string FormulaVersion { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string CalculatedAtUtc { get; set; } = string.Empty;
    }
}
