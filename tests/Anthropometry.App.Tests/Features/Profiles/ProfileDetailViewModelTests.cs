using Anthropometry.App.Features.Profiles;
using Anthropometry.Application.Common;
using Anthropometry.Application.Measurements;
using Anthropometry.App.Tests.Support;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;
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
    public async Task Any_previous_measurement_enables_add_weight()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightOnly, DateTimeOffset.UtcNow));
        var viewModel = CreateViewModel(profile, repository);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(viewModel.CanAddWeight);
    }

    [Fact]
    public void Title_includes_gender_icon_before_profile_name()
    {
        var profile = TestData.Profile();
        var profileDto = new ProfileDto(
            profile.Id,
            "Anna",
            new ProfileSettingsDto(180m, 35, ActivityLevel.Moderate),
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc,
            ProfileGender.Female);

        var viewModel = new ProfileDetailViewModel(
            profileDto,
            new GetMeasurementHistory(new FakeMeasurementRepository()),
            new NavigationSpy(),
            TestData.LanguageService());

        Assert.Equal("♀ Anna", viewModel.Title);
    }

    [Fact]
    public async Task Applying_profile_update_changes_profile_context_used_by_history()
    {
        var profile = TestData.Profile();
        var navigation = new NavigationSpy();
        var viewModel = CreateViewModel(profile, new FakeMeasurementRepository(), navigation);
        var updatedProfile = new ProfileDto(
            profile.Id,
            profile.Name,
            new ProfileSettingsDto(180m, 35, ActivityLevel.High),
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc,
            profile.Gender);

        var method = typeof(ProfileDetailViewModel).GetMethod("ApplyProfileUpdate");
        Assert.NotNull(method);
        method!.Invoke(viewModel, [updatedProfile]);

        await viewModel.HistoryCommand.ExecuteAsync(null);

        Assert.NotNull(navigation.LastHistoryProfile);
        Assert.Equal(ActivityLevel.High, navigation.LastHistoryProfile!.Settings!.ActivityLevel);
    }

    private static ProfileDetailViewModel CreateViewModel(
        Anthropometry.Domain.Profiles.Profile profile,
        FakeMeasurementRepository repository,
        NavigationSpy? navigation = null)
        => new(
            new ProfileDto(
                profile.Id,
                profile.Name,
                new ProfileSettingsDto(180m, 35, ActivityLevel.Moderate),
                profile.CreatedAtUtc,
                profile.UpdatedAtUtc),
            new GetMeasurementHistory(repository),
            navigation ?? new NavigationSpy(),
            TestData.LanguageService());

    private static Measurement CreateMeasurement(Anthropometry.Domain.Profiles.ProfileId profileId, MeasurementType type, DateTimeOffset measuredAtUtc)
        => Measurement.Create(
            profileId,
            type == MeasurementType.WeightOnly
                ? new MeasurementInput(type, 80m, 180m, null, null, 35, ActivityLevel.Moderate, measuredAtUtc)
                : new MeasurementInput(type, 80m, 180m, 40m, 90m, 35, ActivityLevel.Moderate, measuredAtUtc),
            measuredAtUtc).Value;

    private sealed class NavigationSpy : IProfileNavigation
    {
        public ProfileDto? LastHistoryProfile { get; private set; }

        public Task CreateProfileAsync() => Task.CompletedTask;

        public Task RenameProfileAsync(ProfileDto profile) => Task.CompletedTask;

        public Task SelectProfileAsync(ProfileDto profile) => Task.CompletedTask;

        public Task<bool> ConfirmDeleteAsync(ProfileDto profile) => Task.FromResult(false);

        public Task CloseEditorAsync(ProfileDto profile) => Task.CompletedTask;

        public Task CreateMeasurementAsync(ProfileDto profile, MeasurementType type) => Task.CompletedTask;

        public Task CancelAsync() => Task.CompletedTask;

        public Task ShowHistoryAsync(ProfileDto profile)
        {
            LastHistoryProfile = profile;
            return Task.CompletedTask;
        }

        public Task ShowSettingsAsync() => Task.CompletedTask;

        public Task ShowHelpAsync() => Task.CompletedTask;

        public Task ShowGuidanceAsync(Anthropometry.App.Features.Help.GuidanceTopic topic) => Task.CompletedTask;
    }
}
