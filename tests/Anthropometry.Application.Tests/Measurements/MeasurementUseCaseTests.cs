using Anthropometry.Application.Measurements;
using Anthropometry.Application.Tests.Support;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Tests.Measurements;

public sealed class MeasurementUseCaseTests
{
    [Fact]
    public async Task Record_measurement_requires_an_existing_profile()
    {
        var profiles = new FakeProfileRepository();
        var measurements = new FakeMeasurementRepository();
        var command = new RecordMeasurementCommand(ProfileId.New(), TestData.MeasurementInput());

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

        var result = await new RecordMeasurement(profiles, measurements, new FakeClock()).ExecuteAsync(
            new RecordMeasurementCommand(profile.Id, TestData.MeasurementInput()),
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
        var invalidInput = TestData.MeasurementInput() with { WeightKg = 0m };

        var result = await new RecordMeasurement(profiles, measurements, new FakeClock()).ExecuteAsync(
            new RecordMeasurementCommand(profile.Id, invalidInput),
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
}
