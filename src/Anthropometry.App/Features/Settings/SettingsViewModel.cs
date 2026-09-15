using Anthropometry.App.Display;
using Anthropometry.App.Localization;
using Anthropometry.App.Theme;
using CommunityToolkit.Mvvm.ComponentModel;

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
    private string _selectedWeightUnitCode;
    private string _selectedHeightUnitCode;

    public SettingsViewModel(
        LanguageService languageService,
        ThemeService themeService,
        DisplayPreferencesService displayPreferences)
    {
        _languageService = languageService;
        _themeService = themeService;
        _displayPreferences = displayPreferences;
        Languages = LanguageService.SupportedLanguages;
        _selectedLanguage = Languages.Single(language => language.Code == _languageService.CurrentLanguageCode);
        _selectedThemeCode = _themeService.CurrentThemeCode;
        _selectedDateFormatCode = _displayPreferences.DateFormatCode;
        _selectedWeightUnitCode = _displayPreferences.WeightUnitCode;
        _selectedHeightUnitCode = _displayPreferences.HeightUnitCode;
        _languageService.LanguageChanged += OnLanguageChanged;
    }

    public IReadOnlyList<LanguageOption> Languages { get; }

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

    public IReadOnlyList<UnitOption> WeightUnits =>
    [
        new(DisplayPreferencesService.KilogramsCode, _languageService.Get("Kilograms")),
        new(DisplayPreferencesService.PoundsCode, _languageService.Get("Pounds"))
    ];

    public IReadOnlyList<UnitOption> HeightUnits =>
    [
        new(DisplayPreferencesService.CentimetersCode, _languageService.Get("Centimeters")),
        new(DisplayPreferencesService.InchesCode, _languageService.Get("Inches"))
    ];

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

    public UnitOption? SelectedWeightUnit
    {
        get => WeightUnits.SingleOrDefault(option => option.Code == _selectedWeightUnitCode);
        set
        {
            if (value is null || string.Equals(_selectedWeightUnitCode, value.Code, StringComparison.Ordinal))
            {
                return;
            }

            _selectedWeightUnitCode = value.Code;
            OnPropertyChanged();
            _displayPreferences.SetWeightUnit(value.Code);
        }
    }

    public UnitOption? SelectedHeightUnit
    {
        get => HeightUnits.SingleOrDefault(option => option.Code == _selectedHeightUnitCode);
        set
        {
            if (value is null || string.Equals(_selectedHeightUnitCode, value.Code, StringComparison.Ordinal))
            {
                return;
            }

            _selectedHeightUnitCode = value.Code;
            OnPropertyChanged();
            _displayPreferences.SetHeightUnit(value.Code);
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(Themes));
        OnPropertyChanged(nameof(SelectedTheme));
        OnPropertyChanged(nameof(DateFormats));
        OnPropertyChanged(nameof(SelectedDateFormat));
        OnPropertyChanged(nameof(WeightUnits));
        OnPropertyChanged(nameof(SelectedWeightUnit));
        OnPropertyChanged(nameof(HeightUnits));
        OnPropertyChanged(nameof(SelectedHeightUnit));
    }
}
