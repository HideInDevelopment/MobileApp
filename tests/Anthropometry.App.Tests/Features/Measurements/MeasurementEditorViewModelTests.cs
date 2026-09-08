using Anthropometry.App.Features.Measurements;
using Anthropometry.Application.Calculations;
using Anthropometry.Application.Measurements;
using Anthropometry.App.Tests.Support;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Calculations.Bmr;
using Anthropometry.Domain.Calculations.BodyFat;
using Anthropometry.Domain.Calculations.Tdee;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.App.Tests.Features.Measurements;

public sealed class MeasurementEditorViewModelTests
{
    [Fact]
    public async Task Save_is_disabled_until_all_metric_fields_and_activity_are_valid()
    {
        var profile = TestData.Profile();
        var profiles = new FakeProfileRepository();
        profiles.Items.Add(profile);
        var measurements = new FakeMeasurementRepository();
        var results = new FakeCalculationResultRepository();
        var viewModel = CreateViewModel(profile.Id, profiles, measurements, results);

        viewModel.WeightText = "80";
        viewModel.HeightText = "180";
        viewModel.NeckText = "40";
        viewModel.AbdomenText = "90";
        viewModel.AgeText = "35";

        Assert.False(viewModel.CanSave);
        await viewModel.SaveCommand.ExecuteAsync(null);
        Assert.Empty(measurements.Items);
    }

    [Fact]
    public async Task Save_records_measurement_and_calculates_three_results()
    {
        var profile = TestData.Profile();
        var profiles = new FakeProfileRepository();
        profiles.Items.Add(profile);
        var measurements = new FakeMeasurementRepository();
        var results = new FakeCalculationResultRepository();
        var navigation = new NavigationSpy();
        var viewModel = CreateViewModel(profile.Id, profiles, measurements, results, navigation);
        viewModel.WeightText = "80";
        viewModel.HeightText = "180";
        viewModel.NeckText = "40";
        viewModel.AbdomenText = "90";
        viewModel.AgeText = "35";
        viewModel.SelectedActivityLevel = ActivityLevel.Moderate;

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsCompleted);
        Assert.Single(measurements.Items);
        Assert.Equal(3, results.Items.Count);
        Assert.Equal(measurements.Items[0].Id, navigation.SavedMeasurement!.Id);
    }

    private static MeasurementEditorViewModel CreateViewModel(
        ProfileId profileId,
        FakeProfileRepository profiles,
        FakeMeasurementRepository measurements,
        FakeCalculationResultRepository results,
        NavigationSpy? navigation = null)
    {
        var catalog = new FormulaCatalog(new UsNavyMaleBodyFatFormula(), new MifflinStJeorMaleBmrFormula(), new TdeeFormula());
        var clock = new FakeClock();
        return new MeasurementEditorViewModel(
            new RecordMeasurement(profiles, measurements, new FakeClock()),
            new CalculateBodyFat(measurements, results, catalog, clock),
            new CalculateBasalMetabolicRate(measurements, results, catalog, clock),
            new CalculateTotalDailyEnergyExpenditure(measurements, results, catalog, clock),
            profileId,
            navigation ?? new NavigationSpy());
    }

    private sealed class NavigationSpy : IMeasurementNavigation
    {
        public Anthropometry.Application.Common.MeasurementDto? SavedMeasurement { get; private set; }

        public Task ShowResultsAsync(Anthropometry.Application.Common.MeasurementDto measurement)
        {
            SavedMeasurement = measurement;
            return Task.CompletedTask;
        }

        public Task CancelAsync() => Task.CompletedTask;
    }
}
