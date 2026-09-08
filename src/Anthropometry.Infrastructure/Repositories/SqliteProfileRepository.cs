using Anthropometry.Application.Abstractions;
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
            return connection.Query<ProfileRow>("SELECT Id, Name, CreatedAtUtc, UpdatedAtUtc FROM Profiles ORDER BY Name")
                .Select(Map)
                .ToArray();
        }, cancellationToken);

    public Task<Profile?> GetByIdAsync(ProfileId id, CancellationToken cancellationToken)
        => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = _connectionFactory.Create();
            var row = connection.FindWithQuery<ProfileRow>("SELECT Id, Name, CreatedAtUtc, UpdatedAtUtc FROM Profiles WHERE Id = ?", id.ToString());
            return row is null ? null : Map(row);
        }, cancellationToken);

    public Task AddAsync(Profile profile, CancellationToken cancellationToken)
        => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = _connectionFactory.Create();
            connection.Execute(
                "INSERT INTO Profiles (Id, Name, CreatedAtUtc, UpdatedAtUtc) VALUES (?, ?, ?, ?)",
                profile.Id.ToString(),
                profile.Name,
                SqliteValueConverter.ToUtcString(profile.CreatedAtUtc),
                SqliteValueConverter.ToUtcString(profile.UpdatedAtUtc));
        }, cancellationToken);

    public Task UpdateAsync(Profile profile, CancellationToken cancellationToken)
        => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = _connectionFactory.Create();
            var changes = connection.Execute(
                "UPDATE Profiles SET Name = ?, UpdatedAtUtc = ? WHERE Id = ?",
                profile.Name,
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
            SqliteValueConverter.ToUtcDateTimeOffset(row.CreatedAtUtc),
            SqliteValueConverter.ToUtcDateTimeOffset(row.UpdatedAtUtc));
        return result.IsSuccess ? result.Value : throw new InvalidDataException(result.Error!.Code);
    }

    private sealed class ProfileRow
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CreatedAtUtc { get; set; } = string.Empty;
        public string UpdatedAtUtc { get; set; } = string.Empty;
    }
}
