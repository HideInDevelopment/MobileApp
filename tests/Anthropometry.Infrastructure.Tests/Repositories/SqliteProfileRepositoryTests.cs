using Anthropometry.Infrastructure.Persistence.Migrations;
using Anthropometry.Infrastructure.Persistence.Sqlite;
using Anthropometry.Infrastructure.Repositories;
using Anthropometry.Infrastructure.Tests.Support;

namespace Anthropometry.Infrastructure.Tests.Repositories;

public sealed class SqliteProfileRepositoryTests
{
    [Fact]
    public async Task Add_get_and_update_round_trip_profile()
    {
        using var database = new TemporaryDatabase();
        var factory = new SqliteConnectionFactory(database.Path);
        await new MigrationRunner(factory).InitializeAsync(CancellationToken.None);
        var repository = new SqliteProfileRepository(factory);
        var profile = TestData.Profile();

        await repository.AddAsync(profile, CancellationToken.None);
        var loaded = await repository.GetByIdAsync(profile.Id, CancellationToken.None);
        var renamed = profile.Update(
            "Updated",
            Anthropometry.Domain.Profiles.ProfileSettings.Create(181m, 36, Anthropometry.Domain.Calculations.ActivityLevel.High).Value,
            DateTimeOffset.UtcNow.AddMinutes(1));
        await repository.UpdateAsync(profile, CancellationToken.None);

        Assert.True(renamed.IsSuccess);
        Assert.Equal("Manuel", loaded!.Name);
        Assert.Equal("Updated", (await repository.GetByIdAsync(profile.Id, CancellationToken.None))!.Name);
        Assert.Equal(181m, (await repository.GetByIdAsync(profile.Id, CancellationToken.None))!.Settings!.HeightCm);
    }

    [Fact]
    public async Task Delete_with_measurements_removes_measurements_and_results_transactionally()
    {
        using var database = new TemporaryDatabase();
        var factory = new SqliteConnectionFactory(database.Path);
        await new MigrationRunner(factory).InitializeAsync(CancellationToken.None);
        var profiles = new SqliteProfileRepository(factory);
        var measurements = new SqliteMeasurementRepository(factory);
        var results = new SqliteCalculationResultRepository(factory);
        var profile = TestData.Profile();
        var measurement = TestData.Measurement(profile.Id);
        var result = TestData.CalculationResult(measurement.Id);

        await profiles.AddAsync(profile, CancellationToken.None);
        await measurements.AddAsync(measurement, CancellationToken.None);
        await results.AddAsync(result, CancellationToken.None);
        await profiles.DeleteWithMeasurementsAsync(profile.Id, CancellationToken.None);

        Assert.Null(await profiles.GetByIdAsync(profile.Id, CancellationToken.None));
        Assert.Empty(await measurements.GetByProfileAsync(profile.Id, CancellationToken.None));
        Assert.Empty(await results.GetByMeasurementAsync(measurement.Id, CancellationToken.None));
    }
}
