using Anthropometry.App.Features.Help;
using Anthropometry.App.Features.Profiles;
using Anthropometry.Application.Common;
using Anthropometry.Application.Measurements;
using Anthropometry.Application.Profiles;
using Anthropometry.Application.Entitlements;
using Anthropometry.App.Tests.Support;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.App.Tests.Features.Profiles;

public sealed class ProfileTransferMarkupTests
{
    [Fact]
    public async Task Detail_export_command_navigates_with_current_profile()
    {
        var profile = TestData.Profile("Anna");
        var navigation = new NavigationSpy();
        var viewModel = new ProfileDetailViewModel(
            ToDto(profile),
            new GetMeasurementHistory(new FakeMeasurementRepository()),
            navigation,
            TestData.LanguageService(),
            new FakeEntitlementProvider(EntitlementTestData.Premium));

        await viewModel.ExportCommand.ExecuteAsync(null);

        Assert.Equal(profile.Id, navigation.ExportedProfile!.Id);
    }

    [Fact]
    public async Task List_import_command_is_available_for_premium_below_the_profile_limit()
    {
        var repository = new FakeProfileRepository();
        repository.Items.Add(TestData.Profile());
        var viewModel = CreateListViewModel(repository, new NavigationSpy());

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(viewModel.CanImportProfile);
        Assert.True(viewModel.ImportCommand.CanExecute(null));
    }

    [Fact]
    public async Task List_import_command_opens_premium_at_the_profile_limit()
    {
        var repository = new FakeProfileRepository();
        for (var index = 0; index < 10; index++)
        {
            repository.Items.Add(TestData.Profile($"Profile {index}"));
        }

        var viewModel = CreateListViewModel(repository, new NavigationSpy());
        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.False(viewModel.CanImportProfile);
        Assert.True(viewModel.ImportCommand.CanExecute(null));
    }

    [Fact]
    public async Task Successful_import_reloads_the_profile_list()
    {
        var repository = new FakeProfileRepository();
        repository.Items.Add(TestData.Profile("Existing"));
        var navigation = new NavigationSpy { ImportAddsProfile = true, ProfileRepository = repository };
        var viewModel = CreateListViewModel(repository, navigation);
        await viewModel.LoadCommand.ExecuteAsync(null);

        await viewModel.ImportCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Profiles.Count);
    }

    [Fact]
    public async Task Cancelled_import_keeps_the_current_profile_list()
    {
        var repository = new FakeProfileRepository();
        repository.Items.Add(TestData.Profile("Existing"));
        var viewModel = CreateListViewModel(repository, new NavigationSpy());
        await viewModel.LoadCommand.ExecuteAsync(null);

        await viewModel.ImportCommand.ExecuteAsync(null);

        Assert.Single(viewModel.Profiles);
        Assert.Equal("Existing", viewModel.Profiles[0].Name);
    }

    [Fact]
    public void Profile_transfer_actions_are_bound_in_the_profile_views()
    {
        var detail = ReadMarkup("ProfileDetailPage.xaml");
        var list = ReadMarkup("ProfileListPage.xaml");
        var prompt = ReadMarkup("PassphrasePromptPage.xaml");

        Assert.Contains("{DynamicResource ExportProfile}", detail);
        Assert.Contains("Command=\"{Binding ExportCommand}\"", detail);
        Assert.Contains("{DynamicResource ImportProfile}", list);
        Assert.Contains("Command=\"{Binding ImportCommand}\"", list);
        Assert.Contains("IsVisible=\"{Binding IsAddProfileLocked}\"", list);
        Assert.Contains("IsVisible=\"{Binding IsImportProfileLocked}\"", list);
        Assert.Contains("LockedButton", list);
        Assert.Contains("IsExportLocked", detail);
        Assert.DoesNotContain("IsEnabled=\"{Binding CanImportProfile}\"", list);
        Assert.Contains("IsPassword=\"True\"", prompt);
        Assert.Contains("ProfileTransferCodeInstructions", prompt);
    }

    private static ProfileListViewModel CreateListViewModel(FakeProfileRepository repository, NavigationSpy navigation)
        => new(
            new GetProfiles(repository),
            new DeleteProfile(repository),
            navigation,
            TestData.LanguageService(),
            new FakeEntitlementProvider(EntitlementTestData.Premium));

    private static ProfileDto ToDto(Anthropometry.Domain.Profiles.Profile profile)
        => new(
            profile.Id,
            profile.Name,
            new ProfileSettingsDto(180m, 35, Anthropometry.Domain.Calculations.ActivityLevel.Moderate),
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc,
            profile.Gender);

    private static string ReadMarkup(string fileName)
        => File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Features", "Profiles", fileName)));

    private sealed class NavigationSpy : IProfileNavigation
    {
        public ProfileDto? ExportedProfile { get; private set; }
        public bool ImportAddsProfile { get; init; }
        public FakeProfileRepository? ProfileRepository { get; init; }

        public Task CreateProfileAsync() => Task.CompletedTask;
        public Task RenameProfileAsync(ProfileDto profile) => Task.CompletedTask;
        public Task SelectProfileAsync(ProfileDto profile) => Task.CompletedTask;
        public Task<bool> ConfirmDeleteAsync(ProfileDto profile) => Task.FromResult(false);
        public Task CloseEditorAsync(ProfileDto profile) => Task.CompletedTask;
        public Task CreateMeasurementAsync(ProfileDto profile, MeasurementType type) => Task.CompletedTask;
        public Task CancelAsync() => Task.CompletedTask;
        public Task ShowHistoryAsync(ProfileDto profile) => Task.CompletedTask;
        public Task ShowSettingsAsync() => Task.CompletedTask;
        public Task ShowPremiumAsync() => Task.CompletedTask;
        public Task ShowHelpAsync() => Task.CompletedTask;
        public Task ShowGuidanceAsync(GuidanceTopic topic) => Task.CompletedTask;

        public Task ExportProfileAsync(ProfileDto profile)
        {
            ExportedProfile = profile;
            return Task.CompletedTask;
        }

        public Task ImportProfileAsync()
        {
            if (ImportAddsProfile && ProfileRepository is not null)
            {
                ProfileRepository.Items.Add(TestData.Profile("Imported"));
            }

            return Task.CompletedTask;
        }
    }
}
