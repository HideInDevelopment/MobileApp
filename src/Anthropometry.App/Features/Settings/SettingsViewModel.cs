using Anthropometry.App.Localization;
using Anthropometry.App.Theme;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Anthropometry.App.Features.Settings;

public sealed record ThemeOption(string Code, string DisplayName);

public sealed class SettingsViewModel : ObservableObject
{
    private readonly LanguageService _languageService;
    private readonly ThemeService _themeService;
    private LanguageOption? _selectedLanguage;
    private string _selectedThemeCode;

    public SettingsViewModel(LanguageService languageService, ThemeService themeService)
    {
        _languageService = languageService;
        _themeService = themeService;
        Languages = LanguageService.SupportedLanguages;
        _selectedLanguage = Languages.Single(language => language.Code == _languageService.CurrentLanguageCode);
        _selectedThemeCode = _themeService.CurrentThemeCode;
        _languageService.LanguageChanged += OnLanguageChanged;
    }

    public IReadOnlyList<LanguageOption> Languages { get; }

    public IReadOnlyList<ThemeOption> Themes =>
    [
        new(ThemeService.LightCode, _languageService.Get("LightTheme")),
        new(ThemeService.DarkCode, _languageService.Get("DarkTheme"))
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

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(Themes));
        OnPropertyChanged(nameof(SelectedTheme));
    }
}
