using Anthropometry.Application.Measurements;
using Anthropometry.Application.Tests.Support;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Tests.Measurements;

public sealed class GetMeasurementHistoryTests
{
    [Fact]
    public async Task Query_applies_inclusive_utc_date_boundaries_and_returns_newest_first()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightAndSizes, new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero)));
        var from = CreateMeasurement(profile.Id, MeasurementType.WeightOnly, new DateTimeOffset(2026, 9, 2, 0, 0, 0, TimeSpan.Zero));
        var to = CreateMeasurement(profile.Id, MeasurementType.WeightAndSizes, new DateTimeOffset(2026, 9, 3, 12, 0, 0, TimeSpan.Zero));
        repository.Items.Add(from);
        repository.Items.Add(to);

        var result = await new GetMeasurementHistory(repository).ExecuteAsync(
            new MeasurementHistoryQuery(
                profile.Id,
                from.MeasuredAtUtc,
                to.MeasuredAtUtc,
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([to.Id, from.Id], result.Value.Select(measurement => measurement.Id));
    }

    [Fact]
    public async Task Query_filters_by_measurement_type_without_changing_other_history()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        var extended = CreateMeasurement(profile.Id, MeasurementType.WeightAndSizes, DateTimeOffset.UtcNow.AddDays(-1));
        var weightOnly = CreateMeasurement(profile.Id, MeasurementType.WeightOnly, DateTimeOffset.UtcNow);
        repository.Items.Add(extended);
        repository.Items.Add(weightOnly);

        var result = await new GetMeasurementHistory(repository).ExecuteAsync(
            new MeasurementHistoryQuery(profile.Id, null, null, MeasurementType.WeightOnly),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(weightOnly.Id, Assert.Single(result.Value).Id);
    }

    [Fact]
    public async Task Query_without_filters_matches_the_existing_overload()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightAndSizes, DateTimeOffset.UtcNow.AddDays(-1)));
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightOnly, DateTimeOffset.UtcNow));
        var useCase = new GetMeasurementHistory(repository);

        var existing = await useCase.ExecuteAsync(profile.Id, CancellationToken.None);
        var query = await useCase.ExecuteAsync(
            new MeasurementHistoryQuery(profile.Id, null, null, null),
            CancellationToken.None);

        Assert.Equal(existing.Value.Select(measurement => measurement.Id), query.Value.Select(measurement => measurement.Id));
    }

    [Fact]
    public async Task Query_rejects_a_reversed_date_range()
    {
        var result = await new GetMeasurementHistory(new FakeMeasurementRepository()).ExecuteAsync(
            new MeasurementHistoryQuery(
                ProfileId.New(),
                new DateTimeOffset(2026, 9, 10, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 9, 9, 0, 0, 0, TimeSpan.Zero),
                null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("measurementHistory.dateRange.invalid", result.Error!.Code);
    }

    [Fact]
    public async Task Query_returns_an_empty_success_for_a_range_without_matches()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightAndSizes, new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero)));

        var result = await new GetMeasurementHistory(repository).ExecuteAsync(
            new MeasurementHistoryQuery(
                profile.Id,
                new DateTimeOffset(2026, 9, 10, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 9, 11, 0, 0, 0, TimeSpan.Zero),
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    private static Measurement CreateMeasurement(ProfileId profileId, MeasurementType type, DateTimeOffset measuredAtUtc)
        => Measurement.Create(
            profileId,
            type == MeasurementType.WeightOnly
                ? new MeasurementInput(type, 80m, 180m, null, null, 35, ActivityLevel.Moderate, measuredAtUtc)
                : new MeasurementInput(type, 80m, 180m, 40m, 90m, 35, ActivityLevel.Moderate, measuredAtUtc),
            measuredAtUtc).Value;
}
