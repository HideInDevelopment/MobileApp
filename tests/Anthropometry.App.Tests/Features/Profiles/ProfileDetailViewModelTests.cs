using Anthropometry.App.Features.Profiles;
using Anthropometry.Application.Common;
using Anthropometry.Application.Measurements;
using Anthropometry.App.Tests.Support;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;
using Xunit;

namespace Anthropometry.App.Tests.Features.Profiles;

public sealed class ProfileDetailViewModelTests
{
    [Fact]
    public async Task Latest_weight_only_measurement_shows_warning()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightAndSizes, DateTimeOffset.UtcNow.AddDays(-1)));
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightOnly, DateTimeOffset.UtcNow));
        var viewModel = CreateViewModel(profile, repository);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(viewModel.ShowWarningIcon);
    }

    [Fact]
    public async Task Latest_extended_measurement_hides_warning()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightOnly, DateTimeOffset.UtcNow.AddDays(-1)));
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightAndSizes, DateTimeOffset.UtcNow));
        var viewModel = CreateViewModel(profile, repository);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.False(viewModel.ShowWarningIcon);
    }

    [Fact]
    public async Task No_measurements_hides_warning()
    {
        var viewModel = CreateViewModel(TestData.Profile(), new FakeMeasurementRepository());

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.False(viewModel.ShowWarningIcon);
        Assert.Null(viewModel.ErrorMessage);
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
            new NavigationSpy());

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
    }
}
