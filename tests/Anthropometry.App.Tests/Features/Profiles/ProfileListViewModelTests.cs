using Anthropometry.App.Features.Profiles;
using Anthropometry.Application.Profiles;
using Anthropometry.App.Tests.Support;

namespace Anthropometry.App.Tests.Features.Profiles;

public sealed class ProfileListViewModelTests
{
    [Fact]
    public async Task Load_shows_empty_state_when_no_profiles_exist()
    {
        var repository = new FakeProfileRepository();
        var viewModel = new ProfileListViewModel(
            new GetProfiles(repository),
            new DeleteProfile(repository),
            new NavigationSpy(),
            TestData.LanguageService());

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsLoading);
        Assert.True(viewModel.IsEmpty);
        Assert.False(viewModel.HasProfiles);
        Assert.True(viewModel.CanAddProfile);
        Assert.Empty(viewModel.Profiles);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task Load_exposes_profiles_after_success()
    {
        var repository = new FakeProfileRepository();
        repository.Items.Add(TestData.Profile("Manuel"));
        var viewModel = new ProfileListViewModel(
            new GetProfiles(repository),
            new DeleteProfile(repository),
            new NavigationSpy(),
            TestData.LanguageService());

        await viewModel.LoadCommand.ExecuteAsync(null);

        var profile = Assert.Single(viewModel.Profiles);
        Assert.Equal("Manuel", profile.Name);
        Assert.False(viewModel.IsEmpty);
        Assert.True(viewModel.HasProfiles);
        Assert.True(viewModel.CanAddProfile);
    }

    [Fact]
    public async Task Delete_refreshes_profiles_after_confirmation()
    {
        var repository = new FakeProfileRepository();
        var profile = TestData.Profile();
        repository.Items.Add(profile);
        var navigation = new NavigationSpy { ConfirmDeleteResult = true };
        var viewModel = new ProfileListViewModel(new GetProfiles(repository), new DeleteProfile(repository), navigation, TestData.LanguageService());
        await viewModel.LoadCommand.ExecuteAsync(null);

        await viewModel.DeleteCommand.ExecuteAsync(viewModel.Profiles[0]);

        Assert.Empty(viewModel.Profiles);
        Assert.Equal(profile.Id, navigation.ConfirmedProfileId);
    }

    [Fact]
    public async Task Load_shows_error_instead_of_empty_state_when_profiles_cannot_be_read()
    {
        var viewModel = new ProfileListViewModel(
            new GetProfiles(new ThrowingProfileRepository()),
            new DeleteProfile(new ThrowingProfileRepository()),
            new NavigationSpy(),
            TestData.LanguageService());

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsEmpty);
        Assert.Equal("We couldn't load profiles. Try again.", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task Load_disables_add_profile_at_four_profiles()
    {
        var repository = new FakeProfileRepository();
        for (var index = 0; index < 4; index++)
        {
            repository.Items.Add(TestData.Profile($"Profile {index}"));
        }

        var viewModel = new ProfileListViewModel(
            new GetProfiles(repository),
            new DeleteProfile(repository),
            new NavigationSpy(),
            TestData.LanguageService());

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasProfiles);
        Assert.False(viewModel.CanAddProfile);
        Assert.False(viewModel.CreateCommand.CanExecute(null));
    }

    [Fact]
    public async Task Settings_command_opens_settings()
    {
        var navigation = new NavigationSpy();
        var viewModel = new ProfileListViewModel(
            new GetProfiles(new FakeProfileRepository()),
            new DeleteProfile(new FakeProfileRepository()),
            navigation,
            TestData.LanguageService());

        viewModel.SettingsCommand.Execute(null);

        await navigation.SettingsTask;
        Assert.Equal(1, navigation.SettingsCalls);
    }

    private sealed class NavigationSpy : IProfileNavigation
    {
        public bool ConfirmDeleteResult { get; init; }

        public Anthropometry.Domain.Profiles.ProfileId? ConfirmedProfileId { get; private set; }

        public int SettingsCalls { get; private set; }

        public Task SettingsTask { get; private set; } = Task.CompletedTask;

        public Task CreateProfileAsync() => Task.CompletedTask;

        public Task ShowSettingsAsync()
        {
            SettingsCalls++;
            SettingsTask = Task.CompletedTask;
            return SettingsTask;
        }

        public Task RenameProfileAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;

        public Task SelectProfileAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;

        public Task<bool> ConfirmDeleteAsync(Anthropometry.Application.Common.ProfileDto profile)
        {
            ConfirmedProfileId = profile.Id;
            return Task.FromResult(ConfirmDeleteResult);
        }

        public Task CloseEditorAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;

        public Task CreateMeasurementAsync(Anthropometry.Application.Common.ProfileDto profile, Anthropometry.Domain.Measurements.MeasurementType type) => Task.CompletedTask;

        public Task CancelAsync() => Task.CompletedTask;

        public Task ShowHistoryAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;
    }
}
