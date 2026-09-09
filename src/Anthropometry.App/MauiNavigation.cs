using Anthropometry.App.Features.Measurements;
using Anthropometry.App.Features.Profiles;
using Anthropometry.App.Features.Results;
using Anthropometry.Application.Calculations;
using Anthropometry.Application.Common;
using Anthropometry.Application.Measurements;
using Anthropometry.Application.Profiles;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.App;

public sealed class MauiNavigation : IProfileNavigation, IMeasurementNavigation
{
    private readonly IServiceProvider _services;

    public MauiNavigation(IServiceProvider services)
    {
        _services = services;
    }

    public Task CreateProfileAsync()
    {
        var page = new ProfileEditorPage(new ProfileEditorViewModel(
            _services.GetRequiredService<CreateProfile>(),
            _services.GetRequiredService<UpdateProfile>(),
            null,
            this));
        return PushAsync(page);
    }

    public Task SelectProfileAsync(ProfileDto profile)
        => PushAsync(new ProfileDetailPage(profile, this));

    public Task RenameProfileAsync(ProfileDto profile)
    {
        var page = new ProfileEditorPage(new ProfileEditorViewModel(
            _services.GetRequiredService<CreateProfile>(),
            _services.GetRequiredService<UpdateProfile>(),
            profile,
            this));
        return PushAsync(page);
    }

    public Task<bool> ConfirmDeleteAsync(ProfileDto profile)
        => Shell.Current.DisplayAlertAsync(
            "Delete profile",
            $"Delete {profile.Name} and all of its measurements and results?",
            "Delete",
            "Cancel");

    public Task CloseEditorAsync(ProfileDto profile) => PopAsync();

    public Task CreateMeasurementAsync(ProfileDto profile)
    {
        var page = new MeasurementEditorPage(new MeasurementEditorViewModel(
            _services.GetRequiredService<RecordMeasurement>(),
            _services.GetRequiredService<CalculateBodyFat>(),
            _services.GetRequiredService<CalculateBasalMetabolicRate>(),
            _services.GetRequiredService<CalculateTotalDailyEnergyExpenditure>(),
            profile.Id,
            this));
        return PushAsync(page);
    }

    public Task ShowHistoryAsync(ProfileDto profile)
        => PushAsync(new MeasurementHistoryPage(new MeasurementHistoryViewModel(
            _services.GetRequiredService<GetMeasurementHistory>(),
            profile.Id,
            this)));

    public Task ShowResultsAsync(MeasurementDto measurement)
        => PushAsync(new CalculationResultPage(new CalculationResultViewModel(
            _services.GetRequiredService<GetCalculationResults>(),
            measurement.Id)));

    public Task CancelAsync() => PopAsync();

    private static Task PushAsync(Page page) => Shell.Current.Navigation.PushAsync(page);

    private static Task<Page> PopAsync() => Shell.Current.Navigation.PopAsync();
}
