using Anthropometry.Application.Abstractions;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;
using Anthropometry.Infrastructure.Persistence.Sqlite;

namespace Anthropometry.Infrastructure.Repositories;

public sealed class SqliteMeasurementRepository : IMeasurementRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteMeasurementRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Task AddAsync(Measurement measurement, CancellationToken cancellationToken)
        => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = _connectionFactory.Create();
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
        }, cancellationToken);

    public Task UpdateAsync(Measurement measurement, CancellationToken cancellationToken)
        => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = _connectionFactory.Create();
            var changes = connection.Execute(
                "UPDATE Measurements SET ProfileId = ?, MeasuredAtUtc = ?, MeasurementType = ?, WeightKg = ?, HeightCm = ?, NeckCm = ?, AbdomenCm = ?, HipCm = ?, Gender = ?, AgeYears = ?, ActivityLevel = ? WHERE Id = ?",
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
                (int)measurement.ActivityLevel,
                measurement.Id.ToString());
            if (changes == 0)
            {
                throw new KeyNotFoundException($"Measurement {measurement.Id} was not found.");
            }
        }, cancellationToken);

    public Task DeleteWithResultsAsync(MeasurementId id, CancellationToken cancellationToken)
        => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = _connectionFactory.Create();
            connection.RunInTransaction(() =>
            {
                connection.Execute("DELETE FROM CalculationResults WHERE MeasurementId = ?", id.ToString());
                connection.Execute("DELETE FROM Measurements WHERE Id = ?", id.ToString());
            });
        }, cancellationToken);

    public Task<Measurement?> GetByIdAsync(MeasurementId id, CancellationToken cancellationToken)
        => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = _connectionFactory.Create();
            var row = connection.FindWithQuery<MeasurementRow>("SELECT Id, ProfileId, MeasuredAtUtc, MeasurementType, WeightKg, HeightCm, NeckCm, AbdomenCm, HipCm, Gender, AgeYears, ActivityLevel FROM Measurements WHERE Id = ?", id.ToString());
            return row is null ? null : Map(row);
        }, cancellationToken);

    public Task<IReadOnlyList<Measurement>> GetByProfileAsync(ProfileId profileId, CancellationToken cancellationToken)
        => Task.Run<IReadOnlyList<Measurement>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = _connectionFactory.Create();
            return connection.Query<MeasurementRow>(
                    "SELECT Id, ProfileId, MeasuredAtUtc, MeasurementType, WeightKg, HeightCm, NeckCm, AbdomenCm, HipCm, Gender, AgeYears, ActivityLevel FROM Measurements WHERE ProfileId = ? ORDER BY MeasuredAtUtc DESC",
                    profileId.ToString())
                .Select(Map)
                .ToArray();
        }, cancellationToken);

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
}
