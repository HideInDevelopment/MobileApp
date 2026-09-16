using Anthropometry.App.Features.Measurements;
using Anthropometry.App.Features.Profiles;
using Anthropometry.Application.Calculations;
using Anthropometry.Application.Measurements;
using Anthropometry.App.Tests.Support;
using Anthropometry.Domain.Calculations.Bmr;
using Anthropometry.Domain.Calculations.BodyFat;
using Anthropometry.Domain.Calculations.Tdee;

namespace Anthropometry.App.Tests;

public sealed class ResponsiveLayoutTests
{
    [Fact]
    public void Primary_mobile_commands_are_available_without_platform_specific_state()
    {
        var profile = TestData.Profile();
        var profiles = new FakeProfileRepository();
        profiles.Items.Add(profile);
        var measurements = new FakeMeasurementRepository();
        var results = new FakeCalculationResultRepository();
        var catalog = new FormulaCatalog(
            new UsNavyMaleBodyFatFormula(),
            new MifflinStJeorMaleBmrFormula(),
            new TdeeFormula(),
            new UsNavyFemaleBodyFatFormula(),
            new MifflinStJeorFemaleBmrFormula());
        var measurementViewModel = new MeasurementEditorViewModel(
            new RecordMeasurement(profiles, measurements, new FakeClock()),
            new CalculateBodyFat(measurements, results, catalog, new FakeClock()),
            new CalculateBasalMetabolicRate(measurements, results, catalog, new FakeClock()),
            new CalculateTotalDailyEnergyExpenditure(measurements, results, catalog, new FakeClock()),
            new Anthropometry.Application.Common.ProfileDto(
                profile.Id,
                profile.Name,
                new Anthropometry.Application.Common.ProfileSettingsDto(180m, 35, Anthropometry.Domain.Calculations.ActivityLevel.Moderate),
                profile.CreatedAtUtc,
                profile.UpdatedAtUtc),
            Anthropometry.Domain.Measurements.MeasurementType.WeightOnly,
            new NavigationSpy(),
            TestData.LanguageService(),
            TestData.DisplayPreferences());
        var profileViewModel = new ProfileListViewModel(
            new Anthropometry.Application.Profiles.GetProfiles(profiles),
            new Anthropometry.Application.Profiles.DeleteProfile(profiles),
            new ProfileNavigationSpy(),
            TestData.LanguageService());

        Assert.NotNull(measurementViewModel.SaveCommand);
        Assert.NotNull(measurementViewModel.CancelCommand);
        Assert.NotNull(profileViewModel.LoadCommand);
        Assert.NotNull(profileViewModel.CreateCommand);
    }

    [Fact]
    public void Primary_screens_keep_action_targets_at_least_48_pixels_high()
    {
        var root = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Features"));
        var profileList = File.ReadAllText(Path.Combine(root, "Profiles", "ProfileListPage.xaml"));
        var detail = File.ReadAllText(Path.Combine(root, "Profiles", "ProfileDetailPage.xaml"));
        var history = File.ReadAllText(Path.Combine(root, "Measurements", "MeasurementHistoryPage.xaml"));

        Assert.Contains("MinimumHeightRequest=\"48\"", profileList);
        Assert.Contains("MinimumHeightRequest=\"48\"", detail);
        Assert.Contains("MinimumHeightRequest=\"48\"", history);
    }

    private sealed class NavigationSpy : IMeasurementNavigation
    {
        public Task ShowResultsAsync(Anthropometry.Application.Common.MeasurementDto measurement) => Task.CompletedTask;

        public Task ShowResultsAsync(Anthropometry.Application.Common.ProfileDto profile, Anthropometry.Application.Common.MeasurementDto measurement) => Task.CompletedTask;

        public Task EditMeasurementAsync(Anthropometry.Application.Common.ProfileDto profile, Anthropometry.Application.Common.MeasurementDto measurement) => Task.CompletedTask;

        public Task<bool> ConfirmDeleteAsync(Anthropometry.Application.Common.MeasurementDto measurement) => Task.FromResult(false);

        public Task ShowHistoryAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;

        public Task ShowChartOptionsAsync(Anthropometry.Domain.Profiles.ProfileId profileId) => Task.CompletedTask;

        public Task CloseMeasurementAsync() => Task.CompletedTask;

        public Task CancelAsync() => Task.CompletedTask;

        public Task ShowGuidanceAsync(Anthropometry.App.Features.Help.GuidanceTopic topic) => Task.CompletedTask;
    }

    private sealed class ProfileNavigationSpy : IProfileNavigation
    {
        public Task CreateProfileAsync() => Task.CompletedTask;

        public Task RenameProfileAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;

        public Task SelectProfileAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;

        public Task<bool> ConfirmDeleteAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.FromResult(false);

        public Task CloseEditorAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;

        public Task CreateMeasurementAsync(Anthropometry.Application.Common.ProfileDto profile, Anthropometry.Domain.Measurements.MeasurementType type) => Task.CompletedTask;

        public Task CancelAsync() => Task.CompletedTask;

        public Task ShowHistoryAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;

        public Task ShowSettingsAsync() => Task.CompletedTask;

        public Task ShowHelpAsync() => Task.CompletedTask;

        public Task ShowGuidanceAsync(Anthropometry.App.Features.Help.GuidanceTopic topic) => Task.CompletedTask;
    }
}
