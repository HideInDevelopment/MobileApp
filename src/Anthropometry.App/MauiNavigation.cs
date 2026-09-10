using System.Globalization;
using Anthropometry.App.Features.Measurements;
using Anthropometry.App.Features.Profiles;
using Anthropometry.App.Features.Results;
using Anthropometry.App.Features.Settings;
using Anthropometry.App.Localization;
using Anthropometry.Application.Calculations;
using Anthropometry.Application.Common;
using Anthropometry.Application.Measurements;
using Anthropometry.Application.Profiles;
using Anthropometry.Domain.Profiles;
using Anthropometry.Domain.Measurements;

namespace Anthropometry.App;

public sealed class MauiNavigation : IProfileNavigation, IMeasurementNavigation
{
    private readonly IServiceProvider _services;
    private readonly LanguageService _languageService;

    public MauiNavigation(IServiceProvider services, LanguageService languageService)
    {
        _services = services;
        _languageService = languageService;
    }

    public Task CreateProfileAsync()
    {
        var page = new ProfileEditorPage(new ProfileEditorViewModel(
            _services.GetRequiredService<CreateProfile>(),
            _services.GetRequiredService<UpdateProfile>(),
            null,
            this,
            _languageService));
        return PushAsync(page);
    }

    public Task SelectProfileAsync(ProfileDto profile)
        => PushAsync(new ProfileDetailPage(
            profile,
            _services.GetRequiredService<GetMeasurementHistory>(),
            _services.GetRequiredService<GenerateSampleMeasurementHistory>(),
            this,
            _languageService));

    public Task RenameProfileAsync(ProfileDto profile)
    {
        var page = new ProfileEditorPage(new ProfileEditorViewModel(
            _services.GetRequiredService<CreateProfile>(),
            _services.GetRequiredService<UpdateProfile>(),
            profile,
            this,
            _languageService));
        return PushAsync(page);
    }

    public Task<bool> ConfirmDeleteAsync(ProfileDto profile)
        => Shell.Current.DisplayAlertAsync(
            _languageService.Get("DeleteProfileTitle"),
            string.Format(CultureInfo.CurrentCulture, _languageService.Get("DeleteProfileMessage"), profile.Name),
            _languageService.Get("DeleteAction"),
            _languageService.Get("Cancel"));

    public Task CloseEditorAsync(ProfileDto profile) => PopAsync();

    public Task CreateMeasurementAsync(ProfileDto profile, MeasurementType type)
    {
        var page = new MeasurementEditorPage(new MeasurementEditorViewModel(
            _services.GetRequiredService<RecordMeasurement>(),
            _services.GetRequiredService<CalculateBodyFat>(),
            _services.GetRequiredService<CalculateBasalMetabolicRate>(),
            _services.GetRequiredService<CalculateTotalDailyEnergyExpenditure>(),
            profile,
            type,
            this,
            _languageService));
        return PushAsync(page);
    }

    public Task ShowHistoryAsync(ProfileDto profile)
        => PushAsync(new MeasurementHistoryPage(new MeasurementHistoryViewModel(
            _services.GetRequiredService<GetMeasurementHistory>(),
            profile.Id,
            this,
            _languageService)));

    public async Task ShowChartOptionsAsync(ProfileId profileId)
    {
        var weightGraphicLabel = _languageService.Get("WeightGraphic");
        var selection = await Shell.Current.DisplayActionSheetAsync(
            _languageService.Get("Charts"),
            _languageService.Get("Cancel"),
            null,
            weightGraphicLabel);

        if (string.Equals(selection, weightGraphicLabel, StringComparison.Ordinal))
        {
            await PushAsync(new WeightGraphicPage(new WeightGraphicViewModel(
                _services.GetRequiredService<GetMeasurementHistory>(),
                profileId,
                _languageService)));
        }
    }

    public Task ShowSettingsAsync()
        => PushAsync(new SettingsPage(new SettingsViewModel(
            _services.GetRequiredService<LanguageService>())));

    public Task ShowResultsAsync(MeasurementDto measurement)
        => PushAsync(new CalculationResultPage(new CalculationResultViewModel(
            _services.GetRequiredService<GetCalculationResults>(),
            measurement.Id,
            measurement.Type,
            _languageService)));

    public Task CloseMeasurementAsync() => PopAsync();

    public Task CancelAsync() => PopAsync();

    private static Task PushAsync(Page page) => Shell.Current.Navigation.PushAsync(page);

    private static Task<Page> PopAsync() => Shell.Current.Navigation.PopAsync();
}
