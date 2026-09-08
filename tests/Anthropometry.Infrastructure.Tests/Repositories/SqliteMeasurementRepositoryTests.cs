using Anthropometry.Infrastructure.Persistence.Migrations;
using Anthropometry.Infrastructure.Persistence.Sqlite;
using Anthropometry.Infrastructure.Repositories;
using Anthropometry.Infrastructure.Tests.Support;

namespace Anthropometry.Infrastructure.Tests.Repositories;

public sealed class SqliteMeasurementRepositoryTests
{
    [Fact]
    public async Task Get_by_profile_returns_measurements_newest_first()
    {
        using var database = new TemporaryDatabase();
        var factory = new SqliteConnectionFactory(database.Path);
        await new MigrationRunner(factory).InitializeAsync(CancellationToken.None);
        var profiles = new SqliteProfileRepository(factory);
        var repository = new SqliteMeasurementRepository(factory);
        var profile = TestData.Profile();
        var older = TestData.Measurement(profile.Id, DateTimeOffset.UtcNow.AddDays(-1));
        var newer = TestData.Measurement(profile.Id, DateTimeOffset.UtcNow);

        await profiles.AddAsync(profile, CancellationToken.None);
        await repository.AddAsync(older, CancellationToken.None);
        await repository.AddAsync(newer, CancellationToken.None);

        var history = await repository.GetByProfileAsync(profile.Id, CancellationToken.None);

        Assert.Equal(newer.Id, history[0].Id);
        Assert.Equal(older.Id, history[1].Id);
    }

    [Fact]
    public async Task Get_by_id_returns_null_for_unknown_identifier()
    {
        using var database = new TemporaryDatabase();
        var factory = new SqliteConnectionFactory(database.Path);
        await new MigrationRunner(factory).InitializeAsync(CancellationToken.None);
        var repository = new SqliteMeasurementRepository(factory);

        var result = await repository.GetByIdAsync(Anthropometry.Domain.Measurements.MeasurementId.New(), CancellationToken.None);

        Assert.Null(result);
    }
}
