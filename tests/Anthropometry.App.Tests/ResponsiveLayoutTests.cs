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
        var catalog = new FormulaCatalog(new UsNavyMaleBodyFatFormula(), new MifflinStJeorMaleBmrFormula(), new TdeeFormula());
        var measurementViewModel = new MeasurementEditorViewModel(
            new RecordMeasurement(profiles, measurements, new FakeClock()),
            new CalculateBodyFat(measurements, results, catalog, new FakeClock()),
            new CalculateBasalMetabolicRate(measurements, results, catalog, new FakeClock()),
            new CalculateTotalDailyEnergyExpenditure(measurements, results, catalog, new FakeClock()),
            profile.Id,
            new NavigationSpy());
        var profileViewModel = new ProfileListViewModel(
            new Anthropometry.Application.Profiles.GetProfiles(profiles),
            new Anthropometry.Application.Profiles.DeleteProfile(profiles),
            new ProfileNavigationSpy());

        Assert.NotNull(measurementViewModel.SaveCommand);
        Assert.NotNull(measurementViewModel.CancelCommand);
        Assert.NotNull(profileViewModel.LoadCommand);
        Assert.NotNull(profileViewModel.CreateCommand);
    }

    private sealed class NavigationSpy : IMeasurementNavigation
    {
        public Task ShowResultsAsync(Anthropometry.Application.Common.MeasurementDto measurement) => Task.CompletedTask;

        public Task CancelAsync() => Task.CompletedTask;
    }

    private sealed class ProfileNavigationSpy : IProfileNavigation
    {
        public Task CreateProfileAsync() => Task.CompletedTask;

        public Task RenameProfileAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;

        public Task SelectProfileAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;

        public Task<bool> ConfirmDeleteAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.FromResult(false);

        public Task CloseEditorAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;

        public Task CreateMeasurementAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;

        public Task CancelAsync() => Task.CompletedTask;

        public Task ShowHistoryAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;
    }
}
