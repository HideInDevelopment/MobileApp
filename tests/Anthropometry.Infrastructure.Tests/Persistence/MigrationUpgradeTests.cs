using Anthropometry.Infrastructure.Persistence.Migrations;
using Anthropometry.Infrastructure.Persistence.Sqlite;
using Anthropometry.Infrastructure.Repositories;
using Anthropometry.Infrastructure.Tests.Support;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Infrastructure.Tests.Persistence;

public sealed class MigrationUpgradeTests
{
    [Fact]
    public async Task Upgrade_from_schema_one_keeps_existing_data_and_backfills_profile_settings()
    {
        using var database = new TemporaryDatabase();
        var factory = new SqliteConnectionFactory(database.Path);
        await new MigrationRunner(factory, [new Migration0001()]).InitializeAsync(CancellationToken.None);
        var profileId = Guid.NewGuid();
        var measurementId = Guid.NewGuid();
        var resultId = Guid.NewGuid();
        var measuredAt = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero).ToString("O");
        using (var connection = factory.Create())
        {
            connection.Execute(
                "INSERT INTO Profiles (Id, Name, CreatedAtUtc, UpdatedAtUtc) VALUES (?, ?, ?, ?)",
                profileId.ToString(), "Manuel", measuredAt, measuredAt);
            connection.Execute(
                "INSERT INTO Measurements (Id, ProfileId, MeasuredAtUtc, WeightKg, HeightCm, NeckCm, AbdomenCm, AgeYears, ActivityLevel) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)",
                measurementId.ToString(), profileId.ToString(), measuredAt, 80m, 180m, 40m, 90m, 35, (int)ActivityLevel.Moderate);
            connection.Execute(
                "INSERT INTO CalculationResults (Id, MeasurementId, CalculationType, FormulaId, FormulaVersion, Value, Unit, CalculatedAtUtc) VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                resultId.ToString(), measurementId.ToString(), (int)CalculationType.BasalMetabolicRate, "mifflin-st-jeor-male-bmr", "1.0", 1755m, "kcal/day", measuredAt);
        }

        await new MigrationRunner(factory).InitializeAsync(CancellationToken.None);

        var profiles = new SqliteProfileRepository(factory);
        var measurements = new SqliteMeasurementRepository(factory);
        var results = new SqliteCalculationResultRepository(factory);
        var profile = await profiles.GetByIdAsync(new ProfileId(profileId), CancellationToken.None);
        var loadedMeasurement = Assert.Single(await measurements.GetByProfileAsync(new ProfileId(profileId), CancellationToken.None));
        var loadedResults = await results.GetByMeasurementAsync(loadedMeasurement.Id, CancellationToken.None);
        using var schemaConnection = factory.Create();
        var version = schemaConnection.ExecuteScalar<string>("SELECT Value FROM SchemaMetadata WHERE Key = 'schema.version'");

        Assert.NotNull(profile);
        Assert.Equal(profileId, profile!.Id.Value);
        Assert.Equal("Manuel", profile.Name);
        Assert.Equal(180m, profile.Settings!.HeightCm);
        Assert.Equal(35, profile.Settings.AgeYears);
        Assert.Equal(ActivityLevel.Moderate, profile.Settings.ActivityLevel);
        Assert.Equal(MeasurementType.WeightAndSizes, loadedMeasurement.Type);
        Assert.Equal(40m, loadedMeasurement.NeckCm);
        Assert.Equal(90m, loadedMeasurement.AbdomenCm);
        Assert.Single(loadedResults);
        Assert.Equal("mifflin-st-jeor-male-bmr", loadedResults[0].FormulaId);
        Assert.Equal("2", version);
    }
}
