using Anthropometry.Application.Abstractions;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Profiles;
using Anthropometry.Infrastructure.Persistence.Sqlite;
using SQLite;

namespace Anthropometry.Infrastructure.Repositories;

public sealed class SqliteProfileRepository : IProfileRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteProfileRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Task<IReadOnlyList<Profile>> GetAllAsync(CancellationToken cancellationToken)
        => Task.Run<IReadOnlyList<Profile>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = _connectionFactory.Create();
            return connection.Query<ProfileRow>("SELECT Id, Name, Gender, HeightCm, AgeYears, ActivityLevel, CreatedAtUtc, UpdatedAtUtc FROM Profiles ORDER BY Name")
                .Select(Map)
                .ToArray();
        }, cancellationToken);

    public Task<Profile?> GetByIdAsync(ProfileId id, CancellationToken cancellationToken)
        => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = _connectionFactory.Create();
            var row = connection.FindWithQuery<ProfileRow>("SELECT Id, Name, Gender, HeightCm, AgeYears, ActivityLevel, CreatedAtUtc, UpdatedAtUtc FROM Profiles WHERE Id = ?", id.ToString());
            return row is null ? null : Map(row);
        }, cancellationToken);

    public Task AddAsync(Profile profile, CancellationToken cancellationToken)
        => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = _connectionFactory.Create();
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
        }, cancellationToken);

    public Task UpdateAsync(Profile profile, CancellationToken cancellationToken)
        => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = _connectionFactory.Create();
            var changes = connection.Execute(
                "UPDATE Profiles SET Name = ?, Gender = ?, HeightCm = ?, AgeYears = ?, ActivityLevel = ?, UpdatedAtUtc = ? WHERE Id = ?",
                profile.Name,
                (int)profile.Gender,
                profile.Settings?.HeightCm,
                profile.Settings?.AgeYears,
                profile.Settings is null ? null : (int)profile.Settings.ActivityLevel,
                SqliteValueConverter.ToUtcString(profile.UpdatedAtUtc),
                profile.Id.ToString());
            if (changes == 0)
            {
                throw new KeyNotFoundException($"Profile {profile.Id} was not found.");
            }
        }, cancellationToken);

    public Task DeleteWithMeasurementsAsync(ProfileId id, CancellationToken cancellationToken)
        => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = _connectionFactory.Create();
            connection.RunInTransaction(() =>
            {
                connection.Execute(
                    "DELETE FROM CalculationResults WHERE MeasurementId IN (SELECT Id FROM Measurements WHERE ProfileId = ?)",
                    id.ToString());
                connection.Execute("DELETE FROM Measurements WHERE ProfileId = ?", id.ToString());
                connection.Execute("DELETE FROM Profiles WHERE Id = ?", id.ToString());
            });
        }, cancellationToken);

    private static Profile Map(ProfileRow row)
    {
        var result = Profile.Rehydrate(
            new ProfileId(Guid.Parse(row.Id)),
            row.Name,
            MapSettings(row),
            SqliteValueConverter.ToUtcDateTimeOffset(row.CreatedAtUtc),
            SqliteValueConverter.ToUtcDateTimeOffset(row.UpdatedAtUtc),
            (ProfileGender)row.Gender);
        return result.IsSuccess ? result.Value : throw new InvalidDataException(result.Error!.Code);
    }

    private static ProfileSettings? MapSettings(ProfileRow row)
    {
        if (!row.HeightCm.HasValue && !row.AgeYears.HasValue && !row.ActivityLevel.HasValue)
        {
            return null;
        }

        if (!row.HeightCm.HasValue || !row.AgeYears.HasValue || !row.ActivityLevel.HasValue)
        {
            throw new InvalidDataException("profile.settings.incomplete");
        }

        var settings = ProfileSettings.Create(row.HeightCm.Value, row.AgeYears.Value, (ActivityLevel)row.ActivityLevel.Value);
        return settings.IsSuccess ? settings.Value : throw new InvalidDataException(settings.Error!.Code);
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
}
