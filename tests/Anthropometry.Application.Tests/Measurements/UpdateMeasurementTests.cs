using Anthropometry.Application.Measurements;
using Anthropometry.Application.Tests.Support;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;

namespace Anthropometry.Application.Tests.Measurements;

public sealed class UpdateMeasurementTests
{
    [Fact]
    public async Task Update_preserves_id_updates_values_and_replaces_previous_results()
    {
        var profile = TestData.Profile();
        var measurements = new FakeMeasurementRepository();
        var existing = TestData.Measurement(profile.Id);
        measurements.Items.Add(existing);
        var clock = new FakeClock { UtcNow = new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero) };
        var results = new FakeCalculationResultRepository();
        results.Items.Add(CalculationResult.Create(
            existing.Id,
            CalculationType.BasalMetabolicRate,
            new CalculationResultValue(1755m, "kcal/day", "old-bmr", "1.0"),
            DateTimeOffset.UtcNow).Value);

        var result = await new UpdateMeasurement(measurements, results, clock, new FakeEntitlementProvider(EntitlementTestData.Premium)).ExecuteAsync(
            new UpdateMeasurementCommand(
                existing.Id,
                profile.Id,
                MeasurementType.WeightAndSizes,
                82.5m,
                181.5m,
                41.25m,
                91.75m,
                36,
                Anthropometry.Domain.Calculations.ActivityLevel.High,
                new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(existing.Id, result.Value.Id);
        Assert.Equal(82.5m, result.Value.WeightKg);
        Assert.Equal(181.5m, result.Value.HeightCm);
        Assert.Equal(41.25m, result.Value.NeckCm);
        Assert.Empty(results.Items);
        Assert.Equal(82.5m, measurements.Items.Single().WeightKg);
    }

    [Fact]
    public async Task Update_returns_not_found_for_a_measurement_owned_by_another_profile()
    {
        var owner = TestData.Profile();
        var otherProfile = TestData.Profile("Other");
        var measurements = new FakeMeasurementRepository();
        var existing = TestData.Measurement(owner.Id);
        measurements.Items.Add(existing);

        var result = await new UpdateMeasurement(measurements, new FakeCalculationResultRepository()).ExecuteAsync(
            new UpdateMeasurementCommand(
                existing.Id,
                otherProfile.Id,
                MeasurementType.WeightAndSizes,
                82m,
                180m,
                40m,
                90m,
                35,
                Anthropometry.Domain.Calculations.ActivityLevel.Moderate,
                DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("measurement.notFound", result.Error!.Code);
    }

    [Fact]
    public async Task Free_user_cannot_change_a_measurement_to_a_past_date()
    {
        var profile = TestData.Profile();
        var clock = new FakeClock { UtcNow = new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero) };
        var measurements = new FakeMeasurementRepository();
        var existing = Anthropometry.Domain.Measurements.Measurement.Create(
            profile.Id,
            TestData.MeasurementInput(clock.UtcNow),
            clock.UtcNow).Value;
        measurements.Items.Add(existing);
        var results = new FakeCalculationResultRepository();
        results.Items.Add(CalculationResult.Create(
            existing.Id,
            CalculationType.BasalMetabolicRate,
            new CalculationResultValue(1755m, "kcal/day", "old-bmr", "1.0"),
            clock.UtcNow).Value);

        var result = await new UpdateMeasurement(
            measurements,
            results,
            clock,
            new FakeEntitlementProvider(EntitlementTestData.Free)).ExecuteAsync(
            new UpdateMeasurementCommand(
                existing.Id,
                profile.Id,
                MeasurementType.WeightAndSizes,
                82.5m,
                181.5m,
                41.25m,
                91.75m,
                36,
                Anthropometry.Domain.Calculations.ActivityLevel.High,
                clock.UtcNow.AddDays(-1)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("measurement.pastDate.premiumRequired", result.Error!.Code);
        Assert.Single(results.Items);
        Assert.Equal(existing.WeightKg, measurements.Items.Single().WeightKg);
    }
}
