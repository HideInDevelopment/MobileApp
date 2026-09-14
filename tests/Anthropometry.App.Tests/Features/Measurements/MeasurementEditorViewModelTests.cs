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
    public async Task Weight_only_save_requires_only_weight_and_returns_to_history()
    {
        var profile = TestData.Profile();
        var profiles = new FakeProfileRepository();
        profiles.Items.Add(profile);
        var measurements = new FakeMeasurementRepository();
        measurements.Items.Add(TestData.Measurement(profile.Id));
        var results = new FakeCalculationResultRepository();
        var navigation = new NavigationSpy();
        var viewModel = CreateViewModel(profile, MeasurementType.WeightOnly, profiles, measurements, results, navigation);

        viewModel.WeightText = "79";

        Assert.True(viewModel.CanSave);
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(2, measurements.Items.Count);
        var saved = measurements.Items[1];
        Assert.Equal(MeasurementType.WeightOnly, saved.Type);
        Assert.Null(saved.NeckCm);
        Assert.Null(saved.AbdomenCm);
        Assert.Equal(3, results.Items.Count);
        Assert.Equal(1, navigation.CloseCalls);
        Assert.Equal(1, navigation.HistoryCalls);
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
    public async Task Weight_and_sizes_save_calculates_three_results_and_returns_to_history()
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
        Assert.Null(navigation.SavedMeasurement);
        Assert.Equal(1, navigation.CloseCalls);
        Assert.Equal(1, navigation.HistoryCalls);
    }

    [Fact]
    public async Task Weight_and_sizes_save_preserves_decimal_sizes()
    {
        var profile = TestData.Profile();
        var profiles = new FakeProfileRepository();
        profiles.Items.Add(profile);
        var measurements = new FakeMeasurementRepository();
        var results = new FakeCalculationResultRepository();
        var viewModel = CreateViewModel(profile, MeasurementType.WeightAndSizes, profiles, measurements, results);

        viewModel.WeightText = "80";
        viewModel.NeckText = "40.5";
        viewModel.AbdomenText = "90.25";

        Assert.True(viewModel.CanSave);
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(40.5m, measurements.Items[0].NeckCm);
        Assert.Equal(90.25m, measurements.Items[0].AbdomenCm);
    }

    [Fact]
    public async Task Weight_and_sizes_save_accepts_comma_decimal_sizes()
    {
        var profile = TestData.Profile();
        var profiles = new FakeProfileRepository();
        profiles.Items.Add(profile);
        var measurements = new FakeMeasurementRepository();
        var results = new FakeCalculationResultRepository();
        var languageService = TestData.LanguageService();
        languageService.SetLanguage("en");
        var viewModel = CreateViewModel(
            profile,
            MeasurementType.WeightAndSizes,
            profiles,
            measurements,
            results,
            languageService: languageService);

        viewModel.WeightText = "80";
        viewModel.NeckText = "40,5";
        viewModel.AbdomenText = "90,25";

        Assert.True(viewModel.CanSave);
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(40.5m, measurements.Items[0].NeckCm);
        Assert.Equal(90.25m, measurements.Items[0].AbdomenCm);
    }

    [Fact]
    public async Task Female_weight_and_sizes_save_requires_and_persists_hip()
    {
        var profile = Profile.Create(
            "Anna",
            ProfileSettings.Create(180m, 35, ActivityLevel.Moderate).Value,
            DateTimeOffset.UtcNow,
            ProfileGender.Female).Value;
        var profiles = new FakeProfileRepository();
        profiles.Items.Add(profile);
        var measurements = new FakeMeasurementRepository();
        var results = new FakeCalculationResultRepository();
        var viewModel = CreateViewModel(profile, MeasurementType.WeightAndSizes, profiles, measurements, results);

        viewModel.WeightText = "80";
        viewModel.NeckText = "40";
        viewModel.AbdomenText = "90";

        Assert.False(viewModel.CanSave);

        viewModel.HipText = "110.5";

        Assert.True(viewModel.CanSave);
        await viewModel.SaveCommand.ExecuteAsync(null);

        var saved = Assert.Single(measurements.Items);
        Assert.Equal(ProfileGender.Female, saved.Gender);
        Assert.Equal(110.5m, saved.HipCm);
        Assert.Equal(3, results.Items.Count);
    }

    [Fact]
    public async Task Weight_only_save_recalculates_results_from_previous_sizes_and_returns_to_profile()
    {
        var profile = TestData.Profile();
        var profiles = new FakeProfileRepository();
        profiles.Items.Add(profile);
        var measurements = new FakeMeasurementRepository();
        var previous = TestData.Measurement(profile.Id);
        measurements.Items.Add(previous);
        var results = new FakeCalculationResultRepository();
        results.Items.Add(CalculationResult.Create(
            previous.Id,
            CalculationType.BodyFatPercentage,
            new CalculationResultValue(18m, "%", "old-body-fat", "1.0"),
            DateTimeOffset.UtcNow.AddMinutes(-1)).Value);
        results.Items.Add(CalculationResult.Create(
            previous.Id,
            CalculationType.BasalMetabolicRate,
            new CalculationResultValue(1755m, "kcal/day", "old-bmr", "1.0"),
            DateTimeOffset.UtcNow.AddMinutes(-1)).Value);
        results.Items.Add(CalculationResult.Create(
            previous.Id,
            CalculationType.TotalDailyEnergyExpenditure,
            new CalculationResultValue(2720m, "kcal/day", "old-tdee", "1.0"),
            DateTimeOffset.UtcNow.AddMinutes(-1)).Value);
        var navigation = new NavigationSpy();
        var viewModel = CreateViewModel(
            profile,
            MeasurementType.WeightOnly,
            profiles,
            measurements,
            results,
            navigation);

        viewModel.WeightText = "79";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsCompleted);
        Assert.Equal(2, measurements.Items.Count);
        Assert.Equal(6, results.Items.Count);
        Assert.Equal(3, results.Items.Count(result => result.MeasurementId == previous.Id));
        Assert.Equal(3, results.Items.Count(result => result.MeasurementId == measurements.Items[1].Id));
        Assert.Equal(1745m, results.Items.Single(result => result.MeasurementId == measurements.Items[1].Id && result.CalculationType == CalculationType.BasalMetabolicRate).Value);
        Assert.Equal(1745m * 1.55m, results.Items.Single(result => result.MeasurementId == measurements.Items[1].Id && result.CalculationType == CalculationType.TotalDailyEnergyExpenditure).Value);
        Assert.Null(navigation.SavedMeasurement);
        Assert.Equal(1, navigation.CloseCalls);
        Assert.Equal(1, navigation.HistoryCalls);
    }

    private static MeasurementEditorViewModel CreateViewModel(
        Profile profile,
        MeasurementType measurementType,
        FakeProfileRepository profiles,
        FakeMeasurementRepository measurements,
        FakeCalculationResultRepository results,
        NavigationSpy? navigation = null,
        Anthropometry.App.Localization.LanguageService? languageService = null)
    {
        var catalog = new FormulaCatalog(
            new UsNavyMaleBodyFatFormula(),
            new MifflinStJeorMaleBmrFormula(),
            new TdeeFormula(),
            new UsNavyFemaleBodyFatFormula(),
            new MifflinStJeorFemaleBmrFormula());
        var clock = new FakeClock();
        var profileDto = new ProfileDto(
            profile.Id,
            profile.Name,
            new ProfileSettingsDto(180m, 35, ActivityLevel.Moderate),
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc,
            profile.Gender);
        return new MeasurementEditorViewModel(
            new RecordMeasurement(profiles, measurements, new FakeClock()),
            new CalculateBodyFat(measurements, results, catalog, clock),
            new CalculateBasalMetabolicRate(measurements, results, catalog, clock),
            new CalculateTotalDailyEnergyExpenditure(measurements, results, catalog, clock),
            profileDto,
            measurementType,
            navigation ?? new NavigationSpy(),
            languageService ?? TestData.LanguageService());
    }

    private sealed class NavigationSpy : IMeasurementNavigation
    {
        public MeasurementDto? SavedMeasurement { get; private set; }

        public int CloseCalls { get; private set; }

        public int HistoryCalls { get; private set; }

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

        public Task ShowHistoryAsync(ProfileDto profile)
        {
            HistoryCalls++;
            return Task.CompletedTask;
        }

        public Task ShowChartOptionsAsync(Anthropometry.Domain.Profiles.ProfileId profileId) => Task.CompletedTask;

        public Task CancelAsync() => Task.CompletedTask;
    }
}
