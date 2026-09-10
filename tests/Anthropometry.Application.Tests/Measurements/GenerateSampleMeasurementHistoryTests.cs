using Anthropometry.Application.Calculations;
using Anthropometry.Application.Measurements;
using Anthropometry.Application.Tests.Support;
using Anthropometry.Domain.Calculations.BodyFat;
using Anthropometry.Domain.Calculations.Bmr;
using Anthropometry.Domain.Calculations.Tdee;
using Anthropometry.Domain.Measurements;

namespace Anthropometry.Application.Tests.Measurements;

public sealed class GenerateSampleMeasurementHistoryTests
{
    [Fact]
    public async Task Generates_thirty_alternating_measurements_and_calculation_results_once()
    {
        var clock = new FakeClock
        {
            UtcNow = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero)
        };
        var profile = TestData.Profile();
        var profiles = new FakeProfileRepository();
        profiles.Items.Add(profile);
        var measurements = new FakeMeasurementRepository();
        measurements.Items.Add(Anthropometry.Domain.Measurements.Measurement.Create(
            profile.Id,
            TestData.MeasurementInput(new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.Zero)),
            clock.UtcNow).Value);
        var results = new FakeCalculationResultRepository();
        var catalog = new FormulaCatalog(
            new UsNavyMaleBodyFatFormula(),
            new MifflinStJeorMaleBmrFormula(),
            new TdeeFormula());
        var generator = new GenerateSampleMeasurementHistory(
            profiles,
            measurements,
            new CalculateBodyFat(measurements, results, catalog, clock),
            new CalculateBasalMetabolicRate(measurements, results, catalog, clock),
            new CalculateTotalDailyEnergyExpenditure(measurements, results, catalog, clock),
            clock);

        var firstRun = await generator.ExecuteAsync(
            new GenerateSampleMeasurementHistoryCommand(profile.Id),
            CancellationToken.None);

        Assert.True(firstRun.IsSuccess);
        Assert.Equal(30, firstRun.Value);
        Assert.Equal(31, measurements.Items.Count);
        Assert.Equal(90, results.Items.Count);

        var generated = measurements.Items
            .Where(measurement => measurement.Id != measurements.Items[0].Id)
            .OrderBy(measurement => measurement.MeasuredAtUtc)
            .ToArray();

        Assert.Equal(15, generated.Count(measurement => measurement.Type == MeasurementType.WeightAndSizes));
        Assert.Equal(15, generated.Count(measurement => measurement.Type == MeasurementType.WeightOnly));
        Assert.Equal(new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero), generated[0].MeasuredAtUtc);
        Assert.Equal(new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero), generated[^1].MeasuredAtUtc);
        for (var dayIndex = 0; dayIndex < generated.Length; dayIndex++)
        {
            var measurement = generated[dayIndex];
            Assert.Equal(dayIndex % 2 == 0 ? MeasurementType.WeightAndSizes : MeasurementType.WeightOnly, measurement.Type);
            Assert.InRange(measurement.WeightKg, 77m, 83m);
            if (measurement.Type == MeasurementType.WeightAndSizes)
            {
                Assert.InRange(measurement.NeckCm!.Value, 30m, 50m);
                Assert.InRange(measurement.AbdomenCm!.Value, 80m, 100m);
            }
            else
            {
                Assert.Null(measurement.NeckCm);
                Assert.Null(measurement.AbdomenCm);
            }
        }

        var secondRun = await generator.ExecuteAsync(
            new GenerateSampleMeasurementHistoryCommand(profile.Id),
            CancellationToken.None);

        Assert.True(secondRun.IsSuccess);
        Assert.Equal(0, secondRun.Value);
        Assert.Equal(31, measurements.Items.Count);
        Assert.Equal(90, results.Items.Count);
    }

    [Fact]
    public async Task Requires_an_existing_measurement_with_sizes()
    {
        var clock = new FakeClock();
        var profile = TestData.Profile();
        var profiles = new FakeProfileRepository();
        profiles.Items.Add(profile);
        var measurements = new FakeMeasurementRepository();
        var results = new FakeCalculationResultRepository();
        var catalog = new FormulaCatalog(
            new UsNavyMaleBodyFatFormula(),
            new MifflinStJeorMaleBmrFormula(),
            new TdeeFormula());
        var generator = new GenerateSampleMeasurementHistory(
            profiles,
            measurements,
            new CalculateBodyFat(measurements, results, catalog, clock),
            new CalculateBasalMetabolicRate(measurements, results, catalog, clock),
            new CalculateTotalDailyEnergyExpenditure(measurements, results, catalog, clock),
            clock);

        var result = await generator.ExecuteAsync(
            new GenerateSampleMeasurementHistoryCommand(profile.Id),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("sampleData.sizeMeasurement.required", result.Error!.Code);
        Assert.Empty(measurements.Items);
        Assert.Empty(results.Items);
    }
}
