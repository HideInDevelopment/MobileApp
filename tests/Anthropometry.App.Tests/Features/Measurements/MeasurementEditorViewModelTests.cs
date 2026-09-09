using Anthropometry.App.Features.Measurements;
using Anthropometry.Application.Calculations;
using Anthropometry.Application.Common;
using Anthropometry.Application.Measurements;
using Anthropometry.App.Tests.Support;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;
using Anthropometry.Domain.Calculations.Bmr;
using Anthropometry.Domain.Calculations.BodyFat;
using Anthropometry.Domain.Calculations.Tdee;

namespace Anthropometry.App.Tests.Features.Measurements;

public sealed class MeasurementEditorViewModelTests
{
    [Fact]
    public async Task Weight_only_save_requires_only_weight()
    {
        var profile = TestData.Profile();
        var profiles = new FakeProfileRepository();
        profiles.Items.Add(profile);
        var measurements = new FakeMeasurementRepository();
        var results = new FakeCalculationResultRepository();
        var navigation = new NavigationSpy();
        var viewModel = CreateViewModel(profile, MeasurementType.WeightOnly, profiles, measurements, results, navigation);

        viewModel.WeightText = "79";

        Assert.True(viewModel.CanSave);
        await viewModel.SaveCommand.ExecuteAsync(null);

        var saved = Assert.Single(measurements.Items);
        Assert.Equal(MeasurementType.WeightOnly, saved.Type);
        Assert.Null(saved.NeckCm);
        Assert.Null(saved.AbdomenCm);
        Assert.Empty(results.Items);
        Assert.Equal(1, navigation.CloseCalls);
        Assert.Null(navigation.SavedMeasurement);
    }

    [Fact]
    public async Task Weight_and_sizes_save_requires_both_sizes()
    {
        var profile = TestData.Profile();
        var profiles = new FakeProfileRepository();
        profiles.Items.Add(profile);
        var measurements = new FakeMeasurementRepository();
        var results = new FakeCalculationResultRepository();
        var viewModel = CreateViewModel(profile, MeasurementType.WeightAndSizes, profiles, measurements, results);

        viewModel.WeightText = "80";
        viewModel.NeckText = "40";

        Assert.False(viewModel.CanSave);
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Empty(measurements.Items);
        Assert.Empty(results.Items);
    }

    [Fact]
    public async Task Weight_and_sizes_save_calculates_three_results()
    {
        var profile = TestData.Profile();
        var profiles = new FakeProfileRepository();
        profiles.Items.Add(profile);
        var measurements = new FakeMeasurementRepository();
        var results = new FakeCalculationResultRepository();
        var navigation = new NavigationSpy();
        var viewModel = CreateViewModel(profile, MeasurementType.WeightAndSizes, profiles, measurements, results, navigation);

        viewModel.WeightText = "80";
        viewModel.NeckText = "40";
        viewModel.AbdomenText = "90";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsCompleted);
        Assert.Single(measurements.Items);
        Assert.Equal(3, results.Items.Count);
        Assert.Equal(measurements.Items[0].Id, navigation.SavedMeasurement!.Id);
        Assert.Equal(0, navigation.CloseCalls);
    }

    [Fact]
    public async Task Weight_only_save_does_not_navigate_to_results()
    {
        var profile = TestData.Profile();
        var profiles = new FakeProfileRepository();
        profiles.Items.Add(profile);
        var navigation = new NavigationSpy();
        var viewModel = CreateViewModel(
            profile,
            MeasurementType.WeightOnly,
            profiles,
            new FakeMeasurementRepository(),
            new FakeCalculationResultRepository(),
            navigation);

        viewModel.WeightText = "79";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Null(navigation.SavedMeasurement);
        Assert.Equal(1, navigation.CloseCalls);
    }

    private static MeasurementEditorViewModel CreateViewModel(
        Profile profile,
        MeasurementType measurementType,
        FakeProfileRepository profiles,
        FakeMeasurementRepository measurements,
        FakeCalculationResultRepository results,
        NavigationSpy? navigation = null)
    {
        var catalog = new FormulaCatalog(new UsNavyMaleBodyFatFormula(), new MifflinStJeorMaleBmrFormula(), new TdeeFormula());
        var clock = new FakeClock();
        var profileDto = new ProfileDto(
            profile.Id,
            profile.Name,
            new ProfileSettingsDto(180m, 35, ActivityLevel.Moderate),
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc);
        return new MeasurementEditorViewModel(
            new RecordMeasurement(profiles, measurements, new FakeClock()),
            new CalculateBodyFat(measurements, results, catalog, clock),
            new CalculateBasalMetabolicRate(measurements, results, catalog, clock),
            new CalculateTotalDailyEnergyExpenditure(measurements, results, catalog, clock),
            profileDto,
            measurementType,
            navigation ?? new NavigationSpy());
    }

    private sealed class NavigationSpy : IMeasurementNavigation
    {
        public MeasurementDto? SavedMeasurement { get; private set; }

        public int CloseCalls { get; private set; }

        public Task ShowResultsAsync(MeasurementDto measurement)
        {
            SavedMeasurement = measurement;
            return Task.CompletedTask;
        }

        public Task CloseMeasurementAsync()
        {
            CloseCalls++;
            return Task.CompletedTask;
        }

        public Task CancelAsync() => Task.CompletedTask;
    }
}
