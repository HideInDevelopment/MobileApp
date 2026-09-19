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

    public Task CloseEditorAsync(ProfileDto profile)
    {
        var navigationStack = Shell.Current.Navigation.NavigationStack;
        var previousPage = navigationStack.Count < 2
            ? null
            : navigationStack[navigationStack.Count - 2];
        if (previousPage?.BindingContext is ProfileDetailViewModel viewModel)
        {
            viewModel.ApplyProfileUpdate(profile);
        }

        return PopAsync();
    }

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

    public Task ShowWeightGraphicAsync(ProfileId profileId)
        => PushAsync(new WeightGraphicPage(new WeightGraphicViewModel(
            _services.GetRequiredService<GetMetricHistory>(),
            profileId,
            _languageService,
            _displayPreferences)));

    public Task ShowSettingsAsync()
        => PushAsync(new SettingsPage(new SettingsViewModel(
            _services.GetRequiredService<LanguageService>(),
            _services.GetRequiredService<ThemeService>(),
            _displayPreferences,
            _services.GetRequiredService<ReminderCoordinator>())));

    public async Task ExportProfileAsync(ProfileDto profile)
    {
        var exported = await _services.GetRequiredService<ExportProfile>().ExecuteAsync(
            new ExportProfileCommand(profile.Id),
            CancellationToken.None);
        if (!exported.IsSuccess)
        {
            await Shell.Current.DisplayAlertAsync(
                _languageService.Get("ExportProfileTitle"),
                _languageService.Get("ProfileTransferExportError"),
                _languageService.Get("Close"));
            return;
        }

        var confirmed = await Shell.Current.DisplayAlertAsync(
            _languageService.Get("ExportProfileTitle"),
            _languageService.Get("ProfileTransferPrivacyWarning"),
            _languageService.Get("ExportProfile"),
            _languageService.Get("Cancel"));
        if (!confirmed)
        {
            return;
        }

        try
        {
            await _services.GetRequiredService<IProfileTransferFileService>().ShareAsync(
                exported.Value,
                _languageService.Get("ExportProfileTitle"),
                CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync(
                _languageService.Get("ExportProfileTitle"),
                _languageService.Get("ProfileTransferExportError"),
                _languageService.Get("Close"));
        }
    }

    public async Task ImportProfileAsync()
    {
        Stream? content;
        try
        {
            content = await _services.GetRequiredService<IProfileTransferFileService>().PickCsvAsync(
                _languageService.Get("ImportProfileTitle"),
                CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync(
                _languageService.Get("ImportProfileTitle"),
                _languageService.Get("ProfileTransferInvalidFile"),
                _languageService.Get("Close"));
            return;
        }

        if (content is null)
        {
            return;
        }

        await using (content)
        {
            var useCase = _services.GetRequiredService<ImportProfile>();
            var preview = await useCase.PreviewAsync(content, CancellationToken.None);
            if (!preview.IsSuccess)
            {
                await ShowImportErrorAsync(preview.Error!.Code);
                return;
            }

            var confirmation = string.Format(
                CultureInfo.CurrentCulture,
                _languageService.Get("ProfileTransferConfirmation"),
                preview.Value.Name,
                preview.Value.MeasurementCount,
                preview.Value.CalculationResultCount);
            var confirmed = await Shell.Current.DisplayAlertAsync(
                _languageService.Get("ImportProfileTitle"),
                confirmation,
                _languageService.Get("ImportProfile"),
                _languageService.Get("Cancel"));
            if (!confirmed)
            {
                return;
            }

            var imported = await useCase.ExecuteAsync(preview.Value, CancellationToken.None);
            if (!imported.IsSuccess)
            {
                await ShowImportErrorAsync(imported.Error!.Code);
                return;
            }

            await Shell.Current.DisplayAlertAsync(
                _languageService.Get("ImportProfileTitle"),
                _languageService.Get("ProfileTransferSuccess"),
                _languageService.Get("Close"));
        }
    }

    private Task ShowImportErrorAsync(string code)
    {
        var key = code switch
        {
            "profile.transfer.format.unsupported" => "ProfileTransferUnsupportedFormat",
            "profile.limit.reached" => "ProfileTransferLimitReached",
            _ => "ProfileTransferImportError"
        };
        return Shell.Current.DisplayAlertAsync(
            _languageService.Get("ImportProfileTitle"),
            _languageService.Get(key),
            _languageService.Get("Close"));
    }

    public Task ShowHelpAsync() => PushAsync(new HelpPage());

    public Task ShowGuidanceAsync(GuidanceTopic topic)
    {
        var content = GuidanceContent.Get(_languageService, topic);
        return Shell.Current.DisplayAlertAsync(content.Title, content.Body, _languageService.Get("Close"));
    }

    public Task ShowResultsAsync(MeasurementDto measurement)
        => ShowResultsPageAsync(measurement);

    public Task ShowResultsAsync(ProfileDto profile, MeasurementDto measurement)
        => ShowResultsPageAsync(measurement, replaceCurrentPage: true);

    private Task ShowResultsPageAsync(
        MeasurementDto measurement,
        bool replaceCurrentPage = false)
    {
        var page = new CalculationResultPage(new CalculationResultViewModel(
            _services.GetRequiredService<GetCalculationResults>(),
            measurement.Id,
            _languageService));

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
