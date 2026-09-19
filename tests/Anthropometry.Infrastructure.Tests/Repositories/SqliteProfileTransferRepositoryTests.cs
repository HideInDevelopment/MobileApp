using Anthropometry.Application.Profiles;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;
using Anthropometry.Infrastructure.Persistence.Migrations;
using Anthropometry.Infrastructure.Persistence.Sqlite;
using Anthropometry.Infrastructure.Repositories;
using Anthropometry.Infrastructure.Tests.Support;

namespace Anthropometry.Infrastructure.Tests.Repositories;

public sealed class SqliteProfileTransferRepositoryTests
{
    [Fact]
    public async Task Get_snapshot_returns_profile_measurements_and_results()
    {
        using var database = new TemporaryDatabase();
        var factory = await CreateFactoryAsync(database);
        var profiles = new SqliteProfileRepository(factory);
        var measurements = new SqliteMeasurementRepository(factory);
        var calculationResults = new SqliteCalculationResultRepository(factory);
        var repository = new SqliteProfileTransferRepository(factory);
        var profile = TestData.Profile("Anna");
        var measurement = TestData.Measurement(profile.Id);
        var result = TestData.CalculationResult(measurement.Id);

        await profiles.AddAsync(profile, CancellationToken.None);
        await measurements.AddAsync(measurement, CancellationToken.None);
        await calculationResults.AddAsync(result, CancellationToken.None);

        var snapshot = await repository.GetSnapshotAsync(profile.Id, CancellationToken.None);

        Assert.NotNull(snapshot);
        Assert.Equal(profile.Id, snapshot.Profile.Id);
        Assert.Single(snapshot.Measurements);
        Assert.Single(snapshot.Results);
        Assert.Equal(measurement.Id, snapshot.Measurements[0].Id);
        Assert.Equal(result.Id, snapshot.Results[0].Id);
    }

    [Fact]
    public async Task Import_inserts_the_complete_profile_graph()
    {
        using var database = new TemporaryDatabase();
        var factory = await CreateFactoryAsync(database);
        var repository = new SqliteProfileTransferRepository(factory);
        var profile = TestData.Profile("Imported");
        var measurement = TestData.Measurement(profile.Id);
        var result = TestData.CalculationResult(measurement.Id);

        await repository.ImportAsync(profile, [measurement], [result], CancellationToken.None);

        var loaded = await repository.GetSnapshotAsync(profile.Id, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal("Imported", loaded.Profile.Name);
        Assert.Single(loaded.Measurements);
        Assert.Single(loaded.Results);
    }

    [Fact]
    public async Task Import_rolls_back_profile_and_children_when_a_child_insert_fails()
    {
        using var database = new TemporaryDatabase();
        var factory = await CreateFactoryAsync(database);
        var profiles = new SqliteProfileRepository(factory);
        var repository = new SqliteProfileTransferRepository(factory);
        var existingProfile = TestData.Profile("Existing");
        var existingMeasurement = TestData.Measurement(existingProfile.Id);
        await profiles.AddAsync(existingProfile, CancellationToken.None);
        await new SqliteMeasurementRepository(factory).AddAsync(existingMeasurement, CancellationToken.None);

        var importedProfile = TestData.Profile("Should roll back");
        var duplicateMeasurement = Measurement.Rehydrate(
            existingMeasurement.Id,
            importedProfile.Id,
            new MeasurementInput(
                MeasurementType.WeightAndSizes,
                81m,
                180m,
                40m,
                90m,
                35,
                ActivityLevel.Moderate,
                DateTimeOffset.UtcNow)).Value;

        await Assert.ThrowsAnyAsync<Exception>(() => repository.ImportAsync(
            importedProfile,
            [duplicateMeasurement],
            [],
            CancellationToken.None));

        Assert.Null(await profiles.GetByIdAsync(importedProfile.Id, CancellationToken.None));
        Assert.Null(await repository.GetSnapshotAsync(importedProfile.Id, CancellationToken.None));
        Assert.NotNull(await profiles.GetByIdAsync(existingProfile.Id, CancellationToken.None));
    }

    private static async Task<SqliteConnectionFactory> CreateFactoryAsync(TemporaryDatabase database)
    {
        var factory = new SqliteConnectionFactory(database.Path);
        await new MigrationRunner(factory).InitializeAsync(CancellationToken.None);
        return factory;
    }
}
