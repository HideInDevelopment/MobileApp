using Anthropometry.App.Features.Profiles;
using Anthropometry.Application.Calculations;
using Anthropometry.Application.Common;
using Anthropometry.Application.Measurements;
using Anthropometry.App.Tests.Support;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Calculations.BodyFat;
using Anthropometry.Domain.Calculations.Bmr;
using Anthropometry.Domain.Calculations.Tdee;
using Anthropometry.Domain.Measurements;
using Xunit;

namespace Anthropometry.App.Tests.Features.Profiles;

public sealed class ProfileDetailViewModelTests
{
    [Fact]
    public async Task Weight_only_measurement_does_not_show_profile_warning_and_keeps_add_weight_enabled()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightAndSizes, DateTimeOffset.UtcNow.AddDays(-1)));
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightOnly, DateTimeOffset.UtcNow));
        var viewModel = CreateViewModel(profile, repository);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(viewModel.CanAddWeight);
    }

    [Fact]
    public async Task Extended_measurement_enables_add_weight()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightOnly, DateTimeOffset.UtcNow.AddDays(-1)));
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightAndSizes, DateTimeOffset.UtcNow));
        var viewModel = CreateViewModel(profile, repository);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(viewModel.CanAddWeight);
    }

    [Fact]
    public async Task No_measurements_disable_add_weight()
    {
        var viewModel = CreateViewModel(TestData.Profile(), new FakeMeasurementRepository());

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.False(viewModel.CanAddWeight);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task Weight_only_measurement_without_previous_extended_measurement_keeps_add_weight_disabled()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightOnly, DateTimeOffset.UtcNow));
        var viewModel = CreateViewModel(profile, repository);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.False(viewModel.CanAddWeight);
    }

    private static ProfileDetailViewModel CreateViewModel(Anthropometry.Domain.Profiles.Profile profile, FakeMeasurementRepository repository)
        => new(
            new ProfileDto(
                profile.Id,
                profile.Name,
                new ProfileSettingsDto(180m, 35, ActivityLevel.Moderate),
                profile.CreatedAtUtc,
                profile.UpdatedAtUtc),
            new GetMeasurementHistory(repository),
            CreateGenerator(),
            new NavigationSpy(),
            TestData.LanguageService());

    private static GenerateSampleMeasurementHistory CreateGenerator()
    {
        var measurements = new FakeMeasurementRepository();
        var results = new FakeCalculationResultRepository();
        var catalog = new FormulaCatalog(
            new UsNavyMaleBodyFatFormula(),
            new MifflinStJeorMaleBmrFormula(),
            new TdeeFormula());
        var clock = new FakeClock();
        return new GenerateSampleMeasurementHistory(
            new FakeProfileRepository(),
            measurements,
            new CalculateBodyFat(measurements, results, catalog, clock),
            new CalculateBasalMetabolicRate(measurements, results, catalog, clock),
            new CalculateTotalDailyEnergyExpenditure(measurements, results, catalog, clock),
            clock);
    }

    private static Measurement CreateMeasurement(Anthropometry.Domain.Profiles.ProfileId profileId, MeasurementType type, DateTimeOffset measuredAtUtc)
        => Measurement.Create(
            profileId,
            type == MeasurementType.WeightOnly
                ? new MeasurementInput(type, 80m, 180m, null, null, 35, ActivityLevel.Moderate, measuredAtUtc)
                : new MeasurementInput(type, 80m, 180m, 40m, 90m, 35, ActivityLevel.Moderate, measuredAtUtc),
            measuredAtUtc).Value;

    private sealed class NavigationSpy : IProfileNavigation
    {
        public Task CreateProfileAsync() => Task.CompletedTask;

        public Task RenameProfileAsync(ProfileDto profile) => Task.CompletedTask;

        public Task SelectProfileAsync(ProfileDto profile) => Task.CompletedTask;

        public Task<bool> ConfirmDeleteAsync(ProfileDto profile) => Task.FromResult(false);

        public Task CloseEditorAsync(ProfileDto profile) => Task.CompletedTask;

        public Task CreateMeasurementAsync(ProfileDto profile, MeasurementType type) => Task.CompletedTask;

        public Task CancelAsync() => Task.CompletedTask;

        public Task ShowHistoryAsync(ProfileDto profile) => Task.CompletedTask;

        public Task ShowSettingsAsync() => Task.CompletedTask;
    }
}
