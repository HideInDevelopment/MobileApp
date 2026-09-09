using Anthropometry.App.Features.Measurements;
using Anthropometry.Application.Measurements;
using Anthropometry.App.Tests.Support;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;

namespace Anthropometry.App.Tests.Features.Measurements;

public sealed class MeasurementHistoryViewModelTests
{
    [Fact]
    public async Task Load_shows_empty_state_for_a_profile_without_measurements()
    {
        var profile = TestData.Profile();
        var viewModel = new MeasurementHistoryViewModel(
            new GetMeasurementHistory(new FakeMeasurementRepository()),
            profile.Id,
            new NavigationSpy());

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsEmpty);
        Assert.Empty(viewModel.Measurements);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task Load_orders_measurements_newest_first()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(Anthropometry.Domain.Measurements.Measurement.Create(profile.Id, new Anthropometry.Domain.Measurements.MeasurementInput(MeasurementType.WeightAndSizes, 80m, 180m, 40m, 90m, 35, ActivityLevel.Moderate, DateTimeOffset.UtcNow.AddDays(-1)), DateTimeOffset.UtcNow).Value);
        repository.Items.Add(TestData.Measurement(profile.Id));
        var viewModel = new MeasurementHistoryViewModel(new GetMeasurementHistory(repository), profile.Id, new NavigationSpy());

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Measurements.Count);
        Assert.True(viewModel.Measurements[0].MeasuredAtUtc > viewModel.Measurements[1].MeasuredAtUtc);
    }

    [Fact]
    public async Task Load_shows_recoverable_error_when_history_cannot_be_read()
    {
        var profile = TestData.Profile();
        var viewModel = new MeasurementHistoryViewModel(
            new GetMeasurementHistory(new ThrowingMeasurementRepository()),
            profile.Id,
            new NavigationSpy());

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal("We couldn't load history. Try again.", viewModel.ErrorMessage);
    }

    private sealed class NavigationSpy : IMeasurementNavigation
    {
        public Task ShowResultsAsync(Anthropometry.Application.Common.MeasurementDto measurement) => Task.CompletedTask;

        public Task CancelAsync() => Task.CompletedTask;
    }
}
