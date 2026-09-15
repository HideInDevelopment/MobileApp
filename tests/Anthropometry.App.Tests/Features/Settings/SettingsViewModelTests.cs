using Anthropometry.App.Features.Settings;
using Anthropometry.App.Display;
using Anthropometry.App.Localization;
using Anthropometry.App.Theme;

namespace Anthropometry.App.Tests.Features.Settings;

public sealed class SettingsViewModelTests
{
    [Fact]
    public void Exposes_english_spanish_and_german_and_restores_the_saved_selection()
    {
        var preferences = new FakeLanguagePreferenceStore { LanguageCode = "de" };
        var service = new LanguageService(preferences);
        service.Initialize();
        var themeService = new ThemeService(new FakeThemePreferenceStore { ThemeCode = "dark" });
        themeService.Initialize();

        var viewModel = new SettingsViewModel(service, themeService, CreateDisplayPreferences());

        Assert.Equal(["en", "es", "de"], viewModel.Languages.Select(language => language.Code));
        Assert.Equal("de", viewModel.SelectedLanguage!.Code);
        Assert.Equal(["light", "dark"], viewModel.Themes.Select(theme => theme.Code));
        Assert.Equal("dark", viewModel.SelectedTheme!.Code);
    }

    [Fact]
    public void Selecting_a_language_applies_and_persists_it()
    {
        var preferences = new FakeLanguagePreferenceStore();
        var service = new LanguageService(preferences);
        service.Initialize();
        var themeService = new ThemeService(new FakeThemePreferenceStore());
        themeService.Initialize();
        var viewModel = new SettingsViewModel(service, themeService, CreateDisplayPreferences());

        viewModel.SelectedLanguage = viewModel.Languages.Single(language => language.Code == "es");

        Assert.Equal("es", service.CurrentLanguageCode);
        Assert.Equal("es", preferences.LanguageCode);
    }

    [Fact]
    public void Selecting_a_theme_applies_and_persists_it()
    {
        var languageService = new LanguageService(new FakeLanguagePreferenceStore());
        languageService.Initialize();
        var preferences = new FakeThemePreferenceStore();
        var themeService = new ThemeService(preferences);
        themeService.Initialize();
        var viewModel = new SettingsViewModel(languageService, themeService, CreateDisplayPreferences());

        viewModel.SelectedTheme = viewModel.Themes.Single(theme => theme.Code == "dark");

        Assert.Equal("dark", themeService.CurrentThemeCode);
        Assert.Equal("dark", preferences.ThemeCode);
    }

    [Fact]
    public void Exposes_and_persists_date_and_unit_preferences()
    {
        var languageService = new LanguageService(new FakeLanguagePreferenceStore());
        languageService.Initialize();
        var displayStore = new FakeDisplayPreferenceStore
        {
            DateFormatCode = DisplayPreferencesService.MonthDayYearCode,
            WeightUnitCode = DisplayPreferencesService.PoundsCode,
            HeightUnitCode = DisplayPreferencesService.InchesCode
        };
        var displayPreferences = new DisplayPreferencesService(displayStore);
        displayPreferences.Initialize();
        var themeService = new ThemeService(new FakeThemePreferenceStore());
        themeService.Initialize();

        var viewModel = new SettingsViewModel(languageService, themeService, displayPreferences);

        Assert.Equal(["dd/MM/yyyy", "MM/dd/yyyy"], viewModel.DateFormats.Select(option => option.Code));
        Assert.Equal(["kg", "lb"], viewModel.WeightUnits.Select(option => option.Code));
        Assert.Equal(["cm", "in"], viewModel.HeightUnits.Select(option => option.Code));
        Assert.Equal("MM/dd/yyyy", viewModel.SelectedDateFormat!.Code);
        Assert.Equal("lb", viewModel.SelectedWeightUnit!.Code);
        Assert.Equal("in", viewModel.SelectedHeightUnit!.Code);

        viewModel.SelectedDateFormat = viewModel.DateFormats[0];
        viewModel.SelectedWeightUnit = viewModel.WeightUnits[0];
        viewModel.SelectedHeightUnit = viewModel.HeightUnits[0];

        Assert.Equal("dd/MM/yyyy", displayStore.DateFormatCode);
        Assert.Equal("kg", displayStore.WeightUnitCode);
        Assert.Equal("cm", displayStore.HeightUnitCode);
    }

    private sealed class FakeLanguagePreferenceStore : ILanguagePreferenceStore
    {
        public string? LanguageCode { get; set; }

        public string? GetLanguageCode() => LanguageCode;

        public void SetLanguageCode(string code) => LanguageCode = code;
    }

    private static DisplayPreferencesService CreateDisplayPreferences()
    {
        var service = new DisplayPreferencesService(new FakeDisplayPreferenceStore());
        service.Initialize();
        return service;
    }

    private sealed class FakeThemePreferenceStore : IThemePreferenceStore
    {
        public string? ThemeCode { get; set; }

        public string? GetThemeCode() => ThemeCode;

        public void SetThemeCode(string code) => ThemeCode = code;
    }

    private sealed class FakeDisplayPreferenceStore : IDisplayPreferenceStore
    {
        public string? DateFormatCode { get; set; }

        public string? WeightUnitCode { get; set; }

        public string? HeightUnitCode { get; set; }

        public string? GetDateFormatCode() => DateFormatCode;

        public string? GetWeightUnitCode() => WeightUnitCode;

        public string? GetHeightUnitCode() => HeightUnitCode;

        public void SetDateFormatCode(string code) => DateFormatCode = code;

        public void SetWeightUnitCode(string code) => WeightUnitCode = code;

        public void SetHeightUnitCode(string code) => HeightUnitCode = code;
    }
}
