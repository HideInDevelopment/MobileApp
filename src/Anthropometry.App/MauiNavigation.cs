using System.Globalization;
using Anthropometry.App.Features.Help;
using Anthropometry.App.Features.Measurements;
using Anthropometry.App.Features.Profiles;
using Anthropometry.App.Features.Results;
using Anthropometry.App.Display;
using Anthropometry.App.Features.Settings;
using Anthropometry.App.Localization;
using Anthropometry.App.Theme;
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
    private readonly DisplayPreferencesService _displayPreferences;

    public MauiNavigation(
        IServiceProvider services,
        LanguageService languageService,
        DisplayPreferencesService displayPreferences)
    {
        _services = services;
        _languageService = languageService;
        _displayPreferences = displayPreferences;
    }

    public Task CreateProfileAsync()
    {
        var page = new ProfileEditorPage(new ProfileEditorViewModel(
            _services.GetRequiredService<CreateProfile>(),
            _services.GetRequiredService<UpdateProfile>(),
            null,
            this,
            _languageService,
            _displayPreferences));
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
            _languageService,
            _displayPreferences));
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
            _languageService,
            _displayPreferences,
            _services.GetRequiredService<UpdateMeasurement>()));
        return PushAsync(page);
    }

    public Task ShowHistoryAsync(ProfileDto profile)
        => PushAsync(new MeasurementHistoryPage(new MeasurementHistoryViewModel(
            _services.GetRequiredService<GetMeasurementHistory>(),
            _services.GetRequiredService<DeleteMeasurement>(),
            profile,
            this,
            _languageService,
            _displayPreferences)));

    public Task EditMeasurementAsync(ProfileDto profile, MeasurementDto measurement)
        => PushAsync(new MeasurementEditorPage(new MeasurementEditorViewModel(
            _services.GetRequiredService<RecordMeasurement>(),
            _services.GetRequiredService<CalculateBodyFat>(),
            _services.GetRequiredService<CalculateBasalMetabolicRate>(),
            _services.GetRequiredService<CalculateTotalDailyEnergyExpenditure>(),
            profile,
            measurement.Type,
            this,
            _languageService,
            _displayPreferences,
            _services.GetRequiredService<UpdateMeasurement>(),
            measurement)));

    public Task<bool> ConfirmDeleteAsync(MeasurementDto measurement)
        => Shell.Current.DisplayAlertAsync(
            _languageService.Get("DeleteMeasurementTitle"),
            string.Format(
                CultureInfo.CurrentCulture,
                _languageService.Get("DeleteMeasurementMessage"),
                _displayPreferences.FormatDate(measurement.MeasuredAtUtc)),
            _languageService.Get("DeleteMeasurement"),
            _languageService.Get("Cancel"));

    public async Task ShowChartOptionsAsync(ProfileId profileId)
    {
        var metricChartLabel = _languageService.Get("MetricChart");
        var selection = await Shell.Current.DisplayActionSheetAsync(
            _languageService.Get("Charts"),
            _languageService.Get("Cancel"),
            null,
            metricChartLabel);

        if (string.Equals(selection, metricChartLabel, StringComparison.Ordinal))
        {
            await PushAsync(new WeightGraphicPage(new WeightGraphicViewModel(
                _services.GetRequiredService<GetMetricHistory>(),
                profileId,
                _languageService,
                _displayPreferences)));
        }
    }

    public Task ShowSettingsAsync()
        => PushAsync(new SettingsPage(new SettingsViewModel(
            _services.GetRequiredService<LanguageService>(),
            _services.GetRequiredService<ThemeService>(),
            _displayPreferences,
            _services.GetRequiredService<ReminderCoordinator>())));

    public Task ShowHelpAsync() => PushAsync(new HelpPage());

    public Task ShowGuidanceAsync(GuidanceTopic topic)
    {
        var content = GuidanceContent.Get(_languageService, topic);
        return Shell.Current.DisplayAlertAsync(content.Title, content.Body, _languageService.Get("Close"));
    }

    public Task ShowResultsAsync(MeasurementDto measurement)
        => ShowResultsPageAsync(null, measurement);

    public Task ShowResultsAsync(ProfileDto profile, MeasurementDto measurement)
        => ShowResultsPageAsync(profile, measurement, replaceCurrentPage: true);

    private Task ShowResultsPageAsync(
        ProfileDto? profile,
        MeasurementDto measurement,
        bool replaceCurrentPage = false)
    {
        var page = new CalculationResultPage(new CalculationResultViewModel(
            _services.GetRequiredService<GetCalculationResults>(),
            measurement.Id,
            measurement.Type,
            _languageService,
            profile,
            this));

        if (!replaceCurrentPage)
        {
            return PushAsync(page);
        }

        var navigationStack = Shell.Current.Navigation.NavigationStack;
        var currentPage = navigationStack.Count == 0
            ? null
            : navigationStack[navigationStack.Count - 1];
        if (currentPage is not MeasurementEditorPage)
        {
            return PushAsync(page);
        }

        Shell.Current.Navigation.InsertPageBefore(page, currentPage);
        Shell.Current.Navigation.RemovePage(currentPage);
        return Task.CompletedTask;
    }

    public Task CloseMeasurementAsync() => PopAsync();

    public Task CancelAsync() => PopAsync();

    private static Task PushAsync(Page page) => Shell.Current.Navigation.PushAsync(page);

    private static Task<Page> PopAsync() => Shell.Current.Navigation.PopAsync();
}
