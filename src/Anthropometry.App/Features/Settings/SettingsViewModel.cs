using Anthropometry.App.Display;
using Anthropometry.App.Localization;
using Anthropometry.App.Theme;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anthropometry.App.Features.Settings;

public sealed record ThemeOption(string Code, string DisplayName);
public sealed record DateFormatOption(string Code, string DisplayName);
public sealed record UnitOption(string Code, string DisplayName);

public sealed class SettingsViewModel : ObservableObject
{
    private readonly LanguageService _languageService;
    private readonly ThemeService _themeService;
    private readonly DisplayPreferencesService _displayPreferences;
    private LanguageOption? _selectedLanguage;
    private string _selectedThemeCode;
    private string _selectedDateFormatCode;
    private string _selectedMeasurementSystemCode;
    private readonly ReminderCoordinator? _reminders;
    private bool _isDailyReminderEnabled;
    private bool _isInactivityReminderEnabled;
    private TimeSpan _reminderTime;
    private int _selectedInactivityDays;
    private string _reminderStatus = string.Empty;

    public SettingsViewModel(
        LanguageService languageService,
        ThemeService themeService,
        DisplayPreferencesService displayPreferences,
        ReminderCoordinator? reminders = null)
    {
        _languageService = languageService;
        _themeService = themeService;
        _displayPreferences = displayPreferences;
        _reminders = reminders;
        Languages = LanguageService.SupportedLanguages;
        _selectedLanguage = Languages.Single(language => language.Code == _languageService.CurrentLanguageCode);
        _selectedThemeCode = _themeService.CurrentThemeCode;
        _selectedDateFormatCode = _displayPreferences.DateFormatCode;
        _selectedMeasurementSystemCode = _displayPreferences.MeasurementSystemCode;
        var reminderSettings = _reminders?.Current ?? ReminderSettings.Default;
        _isDailyReminderEnabled = reminderSettings.DailyEnabled;
        _isInactivityReminderEnabled = reminderSettings.InactivityEnabled;
        _reminderTime = reminderSettings.ReminderTime;
        _selectedInactivityDays = reminderSettings.InactivityDays;
        SelectLanguageCommand = new RelayCommand<string?>(SelectLanguage);
        SelectThemeCommand = new RelayCommand<string?>(SelectTheme);
        SelectDateFormatCommand = new RelayCommand<string?>(SelectDateFormat);
        SelectMeasurementSystemCommand = new RelayCommand<string?>(SelectMeasurementSystem);
        SelectInactivityIntervalCommand = new RelayCommand<string?>(SelectInactivityInterval);
        _languageService.LanguageChanged += OnLanguageChanged;
    }

    public IReadOnlyList<LanguageOption> Languages { get; }

    public IRelayCommand<string?> SelectLanguageCommand { get; }

    public IRelayCommand<string?> SelectThemeCommand { get; }

    public IRelayCommand<string?> SelectDateFormatCommand { get; }

    public IRelayCommand<string?> SelectMeasurementSystemCommand { get; }

    public IRelayCommand<string?> SelectInactivityIntervalCommand { get; }

    public IReadOnlyList<ThemeOption> Themes =>
    [
        new(ThemeService.LightCode, _languageService.Get("LightTheme")),
        new(ThemeService.DarkCode, _languageService.Get("DarkTheme"))
    ];

    public IReadOnlyList<DateFormatOption> DateFormats =>
    [
        new(DisplayPreferencesService.DayMonthYearCode, _languageService.Get("DayMonthYear")),
        new(DisplayPreferencesService.MonthDayYearCode, _languageService.Get("MonthDayYear"))
    ];

    public IReadOnlyList<UnitOption> MeasurementSystems =>
    [
        new(DisplayPreferencesService.MetricCode, _languageService.Get("Metric")),
        new(DisplayPreferencesService.ImperialCode, _languageService.Get("Imperial"))
    ];

    public IReadOnlyList<ReminderIntervalOption> InactivityIntervals =>
    [
        new(7, _languageService.Get("SevenDays")),
        new(14, _languageService.Get("FourteenDays"))
    ];

    public bool IsDailyReminderEnabled
    {
        get => _isDailyReminderEnabled;
        private set => SetProperty(ref _isDailyReminderEnabled, value);
    }

    public bool IsInactivityReminderEnabled
    {
        get => _isInactivityReminderEnabled;
        private set => SetProperty(ref _isInactivityReminderEnabled, value);
    }

    public TimeSpan ReminderTime
    {
        get => _reminderTime;
        set
        {
            if (!SetProperty(ref _reminderTime, value))
            {
                return;
            }

            ApplyReminderSettings();
        }
    }

    public ReminderIntervalOption? SelectedInactivityInterval
    {
        get => InactivityIntervals.SingleOrDefault(option => option.Days == _selectedInactivityDays);
        set
        {
            if (value is null || _selectedInactivityDays == value.Days)
            {
                return;
            }

            _selectedInactivityDays = value.Days;
            OnPropertyChanged();
            ApplyReminderSettings();
        }
    }

    public string ReminderStatus
    {
        get => _reminderStatus;
        private set
        {
            if (SetProperty(ref _reminderStatus, value))
            {
                OnPropertyChanged(nameof(HasReminderStatus));
            }
        }
    }

    public bool HasReminderStatus => !string.IsNullOrWhiteSpace(ReminderStatus);

    public LanguageOption? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (!SetProperty(ref _selectedLanguage, value) || value is null)
            {
                return;
            }

            _languageService.SetLanguage(value.Code);
        }
    }

    public ThemeOption? SelectedTheme
    {
        get => Themes.SingleOrDefault(theme => theme.Code == _selectedThemeCode);
        set
        {
            if (value is null || string.Equals(_selectedThemeCode, value.Code, StringComparison.Ordinal))
            {
                return;
            }

            _selectedThemeCode = value.Code;
            OnPropertyChanged();
            _themeService.SetTheme(value.Code);
        }
    }

    public DateFormatOption? SelectedDateFormat
    {
        get => DateFormats.SingleOrDefault(option => option.Code == _selectedDateFormatCode);
        set
        {
            if (value is null || string.Equals(_selectedDateFormatCode, value.Code, StringComparison.Ordinal))
            {
                return;
            }

            _selectedDateFormatCode = value.Code;
            OnPropertyChanged();
            _displayPreferences.SetDateFormat(value.Code);
        }
    }

    public UnitOption? SelectedMeasurementSystem
    {
        get => MeasurementSystems.SingleOrDefault(option => option.Code == _selectedMeasurementSystemCode);
        set
        {
            if (value is null || string.Equals(_selectedMeasurementSystemCode, value.Code, StringComparison.Ordinal))
            {
                return;
            }

            _selectedMeasurementSystemCode = value.Code;
            OnPropertyChanged();
            _displayPreferences.SetMeasurementSystem(value.Code);
        }
    }

    public async Task<bool> SetDailyReminderEnabledAsync(bool enabled)
        => await SetReminderEnabledAsync(enabled, isDaily: true);

    public async Task<bool> SetInactivityReminderEnabledAsync(bool enabled)
        => await SetReminderEnabledAsync(enabled, isDaily: false);

    private async Task<bool> SetReminderEnabledAsync(bool enabled, bool isDaily)
    {
        var previous = isDaily ? IsDailyReminderEnabled : IsInactivityReminderEnabled;
        if (isDaily)
        {
            IsDailyReminderEnabled = enabled;
        }
        else
        {
            IsInactivityReminderEnabled = enabled;
        }

        if (_reminders is null)
        {
            return true;
        }

        var result = await _reminders.UpdateAsync(CreateReminderSettings());
        if (result)
        {
            ReminderStatus = string.Empty;
            return true;
        }

        if (isDaily)
        {
            IsDailyReminderEnabled = previous;
        }
        else
        {
            IsInactivityReminderEnabled = previous;
        }

        ReminderStatus = _languageService.Get("ReminderPermissionRequired");
        return false;
    }

    private void ApplyReminderSettings()
    {
        _reminders?.ApplySettings(CreateReminderSettings());
    }

    private void SelectLanguage(string? code)
    {
        var option = Languages.SingleOrDefault(language => language.Code == code);
        if (option is not null)
        {
            SelectedLanguage = option;
        }
    }

    private void SelectTheme(string? code)
    {
        var option = Themes.SingleOrDefault(theme => theme.Code == code);
        if (option is not null)
        {
            SelectedTheme = option;
        }
    }

    private void SelectDateFormat(string? code)
    {
        var option = DateFormats.SingleOrDefault(format => format.Code == code);
        if (option is not null)
        {
            SelectedDateFormat = option;
        }
    }

    private void SelectMeasurementSystem(string? code)
    {
        var option = MeasurementSystems.SingleOrDefault(system => system.Code == code);
        if (option is not null)
        {
            SelectedMeasurementSystem = option;
        }
    }

    private void SelectInactivityInterval(string? daysText)
    {
        if (int.TryParse(daysText, out var days))
        {
            SelectedInactivityInterval = InactivityIntervals.SingleOrDefault(interval => interval.Days == days);
        }
    }

    private ReminderSettings CreateReminderSettings()
        => new(
            IsDailyReminderEnabled,
            ReminderTime,
            IsInactivityReminderEnabled,
            _selectedInactivityDays);

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(Themes));
        OnPropertyChanged(nameof(SelectedTheme));
        OnPropertyChanged(nameof(DateFormats));
        OnPropertyChanged(nameof(SelectedDateFormat));
        OnPropertyChanged(nameof(MeasurementSystems));
        OnPropertyChanged(nameof(SelectedMeasurementSystem));
        OnPropertyChanged(nameof(InactivityIntervals));
        OnPropertyChanged(nameof(SelectedInactivityInterval));
    }
}
