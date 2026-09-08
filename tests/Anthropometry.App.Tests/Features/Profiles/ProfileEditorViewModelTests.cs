using Anthropometry.App.Features.Profiles;
using Anthropometry.Application.Profiles;
using Anthropometry.App.Tests.Support;

namespace Anthropometry.App.Tests.Features.Profiles;

public sealed class ProfileEditorViewModelTests
{
    [Fact]
    public async Task Save_rejects_required_name_before_persistence()
    {
        var repository = new FakeProfileRepository();
        var viewModel = new ProfileEditorViewModel(
            new CreateProfile(repository, new FakeClock()),
            new RenameProfile(repository, new FakeClock()),
            null,
            new NavigationSpy());
        viewModel.Name = " ";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("A profile name is required.", viewModel.ValidationMessage);
        Assert.Empty(repository.Items);
    }

    [Fact]
    public async Task Save_creates_profile_and_closes_editor()
    {
        var repository = new FakeProfileRepository();
        var navigation = new NavigationSpy();
        var viewModel = new ProfileEditorViewModel(
            new CreateProfile(repository, new FakeClock()),
            new RenameProfile(repository, new FakeClock()),
            null,
            navigation)
        {
            Name = "Manuel"
        };

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsCompleted);
        Assert.Equal("Manuel", Assert.Single(repository.Items).Name);
        Assert.Equal("Manuel", navigation.ClosedProfile!.Name);
    }

    [Fact]
    public async Task Save_shows_recoverable_error_when_persistence_fails()
    {
        var repository = new ThrowingProfileRepository();
        var viewModel = new ProfileEditorViewModel(
            new CreateProfile(repository, new FakeClock()),
            new RenameProfile(repository, new FakeClock()),
            null,
            new NavigationSpy())
        {
            Name = "Manuel"
        };

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("We couldn't save this profile. Try again.", viewModel.ErrorMessage);
        Assert.False(viewModel.IsCompleted);
    }

    private sealed class NavigationSpy : IProfileNavigation
    {
        public Anthropometry.Application.Common.ProfileDto? ClosedProfile { get; private set; }

        public Task CreateProfileAsync() => Task.CompletedTask;

        public Task RenameProfileAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;

        public Task SelectProfileAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;

        public Task<bool> ConfirmDeleteAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.FromResult(false);

        public Task CloseEditorAsync(Anthropometry.Application.Common.ProfileDto profile)
        {
            ClosedProfile = profile;
            return Task.CompletedTask;
        }

        public Task CreateMeasurementAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;

        public Task CancelAsync() => Task.CompletedTask;

        public Task ShowHistoryAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;
    }

}
