using Anthropometry.Application.Calculations;
using Anthropometry.Application.Tests.Support;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Tests.Calculations;

public sealed class GetMetricHistoryTests
{
    [Fact]
    public async Task Weight_history_returns_all_measurements_oldest_first_with_canonical_units()
    {
        var profile = TestData.Profile();
        var measurements = new FakeMeasurementRepository();
        var results = new FakeCalculationResultRepository();
        var newest = CreateMeasurement(profile.Id, 82m, new DateTimeOffset(2026, 9, 8, 8, 0, 0, TimeSpan.Zero));
        var oldest = CreateMeasurement(profile.Id, 80m, new DateTimeOffset(2026, 9, 6, 8, 0, 0, TimeSpan.Zero));
        measurements.Items.Add(newest);
        measurements.Items.Add(oldest);

        var result = await new GetMetricHistory(measurements, results).ExecuteAsync(
            new MetricHistoryQuery(profile.Id, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([oldest.Id, newest.Id], result.Value.Select(point => point.MeasurementId));
        Assert.Equal([80m, 82m], result.Value.Select(point => point.Value));
        Assert.All(result.Value, point =>
        {
            Assert.Null(point.CalculationType);
            Assert.Equal("kg", point.Unit);
        });
    }

    [Fact]
    public async Task Calculated_metric_history_uses_persisted_results_and_excludes_missing_results()
    {
        var profile = TestData.Profile();
        var measurements = new FakeMeasurementRepository();
        var results = new FakeCalculationResultRepository();
        var older = CreateMeasurement(profile.Id, 80m, new DateTimeOffset(2026, 9, 6, 8, 0, 0, TimeSpan.Zero));
        var newerWithoutResult = CreateMeasurement(profile.Id, 82m, new DateTimeOffset(2026, 9, 8, 8, 0, 0, TimeSpan.Zero));
        var newer = CreateMeasurement(profile.Id, 81m, new DateTimeOffset(2026, 9, 7, 8, 0, 0, TimeSpan.Zero));
        measurements.Items.AddRange([older, newerWithoutResult, newer]);
        results.Items.Add(CreateResult(older, CalculationType.BodyFatPercentage, 19.5m, older.MeasuredAtUtc.AddMinutes(1), "%"));
        results.Items.Add(CreateResult(newer, CalculationType.BodyFatPercentage, 18.75m, newer.MeasuredAtUtc.AddMinutes(1), "%"));
        results.Items.Add(CreateResult(newer, CalculationType.BasalMetabolicRate, 1700m, newer.MeasuredAtUtc.AddMinutes(2), "kcal/day"));

        var result = await new GetMetricHistory(measurements, results).ExecuteAsync(
            new MetricHistoryQuery(profile.Id, CalculationType.BodyFatPercentage, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([older.Id, newer.Id], result.Value.Select(point => point.MeasurementId));
        Assert.Equal([19.5m, 18.75m], result.Value.Select(point => point.Value));
        Assert.All(result.Value, point => Assert.Equal(CalculationType.BodyFatPercentage, point.CalculationType));
    }

    [Fact]
    public async Task Query_applies_inclusive_utc_date_boundaries()
    {
        var profile = TestData.Profile();
        var measurements = new FakeMeasurementRepository();
        var results = new FakeCalculationResultRepository();
        var from = CreateMeasurement(profile.Id, 80m, new DateTimeOffset(2026, 9, 6, 8, 0, 0, TimeSpan.Zero));
        var inside = CreateMeasurement(profile.Id, 81m, new DateTimeOffset(2026, 9, 7, 8, 0, 0, TimeSpan.Zero));
        var to = CreateMeasurement(profile.Id, 82m, new DateTimeOffset(2026, 9, 8, 8, 0, 0, TimeSpan.Zero));
        measurements.Items.AddRange([from, inside, to]);

        var result = await new GetMetricHistory(measurements, results).ExecuteAsync(
            new MetricHistoryQuery(profile.Id, null, from.MeasuredAtUtc, to.MeasuredAtUtc),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([from.Id, inside.Id, to.Id], result.Value.Select(point => point.MeasurementId));
    }

    [Fact]
    public async Task Query_rejects_a_reversed_date_range()
    {
        var result = await new GetMetricHistory(new FakeMeasurementRepository(), new FakeCalculationResultRepository()).ExecuteAsync(
            new MetricHistoryQuery(
                ProfileId.New(),
                null,
                new DateTimeOffset(2026, 9, 10, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 9, 9, 0, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("metricHistory.dateRange.invalid", result.Error!.Code);
    }

    private static Measurement CreateMeasurement(ProfileId profileId, decimal weightKg, DateTimeOffset measuredAtUtc)
        => Measurement.Create(
            profileId,
            new MeasurementInput(MeasurementType.WeightAndSizes, weightKg, 180m, 40m, 90m, 35, ActivityLevel.Moderate, measuredAtUtc),
            measuredAtUtc).Value;

    private static CalculationResult CreateResult(
        Measurement measurement,
        CalculationType type,
        decimal value,
        DateTimeOffset calculatedAtUtc,
        string unit)
        => CalculationResult.Create(
            measurement.Id,
            type,
            new CalculationResultValue(value, unit, "test-formula", "1.0"),
            calculatedAtUtc).Value;
}
