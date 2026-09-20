using Anthropometry.Application.Measurements;
using Anthropometry.Application.Tests.Support;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Tests.Measurements;

public sealed class MeasurementUseCaseTests
{
    [Fact]
    public async Task Record_measurement_requires_an_existing_profile()
    {
        var profiles = new FakeProfileRepository();
        var measurements = new FakeMeasurementRepository();
        var command = new RecordMeasurementCommand(ProfileId.New(), MeasurementType.WeightAndSizes, 80m, 40m, 90m, DateTimeOffset.UtcNow);

        var result = await new RecordMeasurement(profiles, measurements, new FakeClock()).ExecuteAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("profile.notFound", result.Error!.Code);
        Assert.Empty(measurements.Items);
    }

    [Fact]
    public async Task Record_measurement_persists_valid_input()
    {
        var profiles = new FakeProfileRepository();
        var measurements = new FakeMeasurementRepository();
        var profile = TestData.Profile();
        profiles.Items.Add(profile);
        var clock = new FakeClock();

        var result = await new RecordMeasurement(profiles, measurements, clock).ExecuteAsync(
            new RecordMeasurementCommand(profile.Id, MeasurementType.WeightAndSizes, 80m, 40m, 90m, clock.UtcNow),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(measurements.Items);
        Assert.Equal(35, result.Value.AgeYears);
    }

    [Fact]
    public async Task Record_measurement_does_not_persist_invalid_input()
    {
        var profile = TestData.Profile();
        var profiles = new FakeProfileRepository();
        profiles.Items.Add(profile);
        var measurements = new FakeMeasurementRepository();
        var clock = new FakeClock();
        var result = await new RecordMeasurement(profiles, measurements, clock).ExecuteAsync(
            new RecordMeasurementCommand(profile.Id, MeasurementType.WeightAndSizes, 0m, 40m, 90m, clock.UtcNow),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("measurement.weight.invalid", result.Error!.Code);
        Assert.Empty(measurements.Items);
    }

    [Fact]
    public async Task Get_measurement_history_returns_newest_first()
    {
        var profiles = new FakeProfileRepository();
        var measurements = new FakeMeasurementRepository();
        var profile = TestData.Profile();
        profiles.Items.Add(profile);
        measurements.Items.Add(Anthropometry.Domain.Measurements.Measurement.Create(profile.Id, TestData.MeasurementInput(DateTimeOffset.UtcNow.AddDays(-1)), DateTimeOffset.UtcNow).Value);
        measurements.Items.Add(Anthropometry.Domain.Measurements.Measurement.Create(profile.Id, TestData.MeasurementInput(DateTimeOffset.UtcNow), DateTimeOffset.UtcNow).Value);

        var result = await new GetMeasurementHistory(measurements).ExecuteAsync(profile.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value[0].MeasuredAtUtc > result.Value[1].MeasuredAtUtc);
    }

    [Fact]
    public async Task Record_measurement_weight_only_uses_profile_settings_and_persists_no_sizes()
    {
        var profile = TestData.Profile();
        var profiles = new FakeProfileRepository();
        profiles.Items.Add(profile);
        var measurements = new FakeMeasurementRepository();
        var clock = new FakeClock();

        var result = await new RecordMeasurement(profiles, measurements, clock).ExecuteAsync(
            new RecordMeasurementCommand(profile.Id, MeasurementType.WeightOnly, 79m, null, null, clock.UtcNow),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var saved = Assert.Single(measurements.Items);
        Assert.Equal(MeasurementType.WeightOnly, saved.Type);
        Assert.Equal(79m, saved.WeightKg);
        Assert.Equal(180m, saved.HeightCm);
        Assert.Equal(35, saved.AgeYears);
        Assert.Equal(ActivityLevel.Moderate, saved.ActivityLevel);
        Assert.Null(saved.NeckCm);
        Assert.Null(saved.AbdomenCm);
    }

    [Fact]
    public async Task Record_measurement_extended_uses_profile_settings()
    {
        var profile = TestData.Profile();
        var profiles = new FakeProfileRepository();
        profiles.Items.Add(profile);
        var measurements = new FakeMeasurementRepository();
        var clock = new FakeClock();

        var result = await new RecordMeasurement(profiles, measurements, clock).ExecuteAsync(
            new RecordMeasurementCommand(profile.Id, MeasurementType.WeightAndSizes, 80m, 40m, 90m, clock.UtcNow),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(180m, Assert.Single(measurements.Items).HeightCm);
        Assert.Equal(35, measurements.Items[0].AgeYears);
        Assert.Equal(ActivityLevel.Moderate, measurements.Items[0].ActivityLevel);
    }

    [Fact]
    public async Task Record_measurement_rejects_incomplete_profile_settings()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var profile = Profile.Rehydrate(ProfileId.New(), "Legacy", null, timestamp, timestamp).Value;
        var profiles = new FakeProfileRepository();
        profiles.Items.Add(profile);

        var result = await new RecordMeasurement(profiles, new FakeMeasurementRepository(), new FakeClock()).ExecuteAsync(
            new RecordMeasurementCommand(profile.Id, MeasurementType.WeightOnly, 79m, null, null, timestamp),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("profile.settings.required", result.Error!.Code);
    }

    [Fact]
    public async Task Free_user_cannot_record_a_past_measurement()
    {
        var profile = TestData.Profile();
        var profiles = new FakeProfileRepository();
        profiles.Items.Add(profile);
        var measurements = new FakeMeasurementRepository();
        var clock = new FakeClock { UtcNow = new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero) };

        var result = await new RecordMeasurement(
            profiles,
            measurements,
            clock,
            new FakeEntitlementProvider(EntitlementTestData.Free)).ExecuteAsync(
            new RecordMeasurementCommand(
                profile.Id,
                MeasurementType.WeightOnly,
                79m,
                null,
                null,
                clock.UtcNow.AddDays(-1)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("measurement.pastDate.premiumRequired", result.Error!.Code);
        Assert.Empty(measurements.Items);
    }

    [Fact]
    public async Task Future_measurements_are_rejected_for_premium_users()
    {
        var profile = TestData.Profile();
        var profiles = new FakeProfileRepository();
        profiles.Items.Add(profile);
        var measurements = new FakeMeasurementRepository();
        var clock = new FakeClock { UtcNow = new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero) };

        var result = await new RecordMeasurement(
            profiles,
            measurements,
            clock,
            new FakeEntitlementProvider(EntitlementTestData.Premium)).ExecuteAsync(
            new RecordMeasurementCommand(
                profile.Id,
                MeasurementType.WeightOnly,
                79m,
                null,
                null,
                clock.UtcNow.AddDays(1)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("measurement.date.invalid", result.Error!.Code);
        Assert.Empty(measurements.Items);
    }
}
