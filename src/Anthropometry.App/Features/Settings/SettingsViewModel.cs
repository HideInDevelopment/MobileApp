using Anthropometry.App.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Anthropometry.App.Features.Settings;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly LanguageService _languageService;
    private LanguageOption? _selectedLanguage;

    public SettingsViewModel(LanguageService languageService)
    {
        _languageService = languageService;
        Languages = LanguageService.SupportedLanguages;
        _selectedLanguage = Languages.Single(language => language.Code == _languageService.CurrentLanguageCode);
    }

    public IReadOnlyList<LanguageOption> Languages { get; }

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
}
