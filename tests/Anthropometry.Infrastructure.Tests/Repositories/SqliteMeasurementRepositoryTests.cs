using Anthropometry.Infrastructure.Persistence.Migrations;
using Anthropometry.Infrastructure.Persistence.Sqlite;
using Anthropometry.Infrastructure.Repositories;
using Anthropometry.Infrastructure.Tests.Support;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

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

    [Fact]
    public async Task Add_and_get_weight_only_round_trips_missing_sizes()
    {
        using var database = new TemporaryDatabase();
        var factory = new SqliteConnectionFactory(database.Path);
        await new MigrationRunner(factory).InitializeAsync(CancellationToken.None);
        var profiles = new SqliteProfileRepository(factory);
        var repository = new SqliteMeasurementRepository(factory);
        var profile = TestData.Profile();
        var measurement = Measurement.Create(
            profile.Id,
            new MeasurementInput(MeasurementType.WeightOnly, 79m, 180m, null, null, 35, ActivityLevel.Moderate, DateTimeOffset.UtcNow),
            DateTimeOffset.UtcNow).Value;

        await profiles.AddAsync(profile, CancellationToken.None);
        await repository.AddAsync(measurement, CancellationToken.None);

        var loaded = await repository.GetByIdAsync(measurement.Id, CancellationToken.None);

        Assert.Equal(MeasurementType.WeightOnly, loaded!.Type);
        Assert.Null(loaded.NeckCm);
        Assert.Null(loaded.AbdomenCm);
        Assert.Equal(79m, loaded.WeightKg);
    }

    [Fact]
    public async Task Add_and_get_female_measurement_round_trips_hip_and_gender()
    {
        using var database = new TemporaryDatabase();
        var factory = new SqliteConnectionFactory(database.Path);
        await new MigrationRunner(factory).InitializeAsync(CancellationToken.None);
        var profiles = new SqliteProfileRepository(factory);
        var repository = new SqliteMeasurementRepository(factory);
        var profile = Anthropometry.Domain.Profiles.Profile.Create(
            "Anna",
            Anthropometry.Domain.Profiles.ProfileSettings.Create(180m, 35, ActivityLevel.Moderate).Value,
            DateTimeOffset.UtcNow,
            ProfileGender.Female).Value;
        var measurement = Measurement.Create(
            profile.Id,
            new MeasurementInput(
                MeasurementType.WeightAndSizes,
                80m,
                180m,
                40m,
                90m,
                35,
                ActivityLevel.Moderate,
                DateTimeOffset.UtcNow,
                110.5m,
                ProfileGender.Female),
            DateTimeOffset.UtcNow).Value;

        await profiles.AddAsync(profile, CancellationToken.None);
        await repository.AddAsync(measurement, CancellationToken.None);

        var loaded = await repository.GetByIdAsync(measurement.Id, CancellationToken.None);

        Assert.Equal(ProfileGender.Female, loaded!.Gender);
        Assert.Equal(110.5m, loaded.HipCm);
    }

    [Fact]
    public async Task Update_preserves_measurement_id_and_replaces_values()
    {
        using var database = new TemporaryDatabase();
        var factory = new SqliteConnectionFactory(database.Path);
        await new MigrationRunner(factory).InitializeAsync(CancellationToken.None);
        var profiles = new SqliteProfileRepository(factory);
        var repository = new SqliteMeasurementRepository(factory);
        var profile = TestData.Profile();
        var measurement = TestData.Measurement(profile.Id);
        var edited = Measurement.Rehydrate(
            measurement.Id,
            profile.Id,
            new MeasurementInput(
                MeasurementType.WeightAndSizes,
                82.5m,
                181.5m,
                41.25m,
                91.75m,
                36,
                ActivityLevel.High,
                new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero))).Value;

        await profiles.AddAsync(profile, CancellationToken.None);
        await repository.AddAsync(measurement, CancellationToken.None);
        await repository.UpdateAsync(edited, CancellationToken.None);

        var loaded = await repository.GetByIdAsync(measurement.Id, CancellationToken.None);

        Assert.Equal(measurement.Id, loaded!.Id);
        Assert.Equal(82.5m, loaded.WeightKg);
        Assert.Equal(181.5m, loaded.HeightCm);
        Assert.Equal(41.25m, loaded.NeckCm);
        Assert.Equal(new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero), loaded.MeasuredAtUtc);
    }

    [Fact]
    public async Task Delete_with_results_removes_measurement_and_results_transactionally()
    {
        using var database = new TemporaryDatabase();
        var factory = new SqliteConnectionFactory(database.Path);
        await new MigrationRunner(factory).InitializeAsync(CancellationToken.None);
        var profiles = new SqliteProfileRepository(factory);
        var repository = new SqliteMeasurementRepository(factory);
        var results = new SqliteCalculationResultRepository(factory);
        var profile = TestData.Profile();
        var measurement = TestData.Measurement(profile.Id);

        await profiles.AddAsync(profile, CancellationToken.None);
        await repository.AddAsync(measurement, CancellationToken.None);
        await results.AddAsync(TestData.CalculationResult(measurement.Id), CancellationToken.None);
        await repository.DeleteWithResultsAsync(measurement.Id, CancellationToken.None);

        Assert.Null(await repository.GetByIdAsync(measurement.Id, CancellationToken.None));
        Assert.Empty(await results.GetByMeasurementAsync(measurement.Id, CancellationToken.None));
    }
}
