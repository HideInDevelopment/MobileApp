using Anthropometry.App.Features.Settings;
using Anthropometry.App.Localization;

namespace Anthropometry.App.Tests.Features.Settings;

public sealed class SettingsViewModelTests
{
    [Fact]
    public void Exposes_english_spanish_and_german_and_restores_the_saved_selection()
    {
        var preferences = new FakeLanguagePreferenceStore { LanguageCode = "de" };
        var service = new LanguageService(preferences);
        service.Initialize();

        var viewModel = new SettingsViewModel(service);

        Assert.Equal(["en", "es", "de"], viewModel.Languages.Select(language => language.Code));
        Assert.Equal("de", viewModel.SelectedLanguage!.Code);
    }

    [Fact]
    public void Selecting_a_language_applies_and_persists_it()
    {
        var preferences = new FakeLanguagePreferenceStore();
        var service = new LanguageService(preferences);
        service.Initialize();
        var viewModel = new SettingsViewModel(service);

        viewModel.SelectedLanguage = viewModel.Languages.Single(language => language.Code == "es");

        Assert.Equal("es", service.CurrentLanguageCode);
        Assert.Equal("es", preferences.LanguageCode);
    }

    private sealed class FakeLanguagePreferenceStore : ILanguagePreferenceStore
    {
        public string? LanguageCode { get; set; }

        public string? GetLanguageCode() => LanguageCode;

        public void SetLanguageCode(string code) => LanguageCode = code;
    }
}
