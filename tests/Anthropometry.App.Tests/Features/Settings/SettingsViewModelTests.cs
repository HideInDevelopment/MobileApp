using System.Globalization;
using Anthropometry.App.Features.Settings;
using Anthropometry.App.Display;
using Anthropometry.App.Localization;
using Anthropometry.App.Theme;

namespace Anthropometry.App.Tests.Features.Settings;

public sealed class SettingsViewModelTests
{
    [Fact]
    public void Exposes_supported_languages_and_restores_the_saved_selection()
    {
        var preferences = new FakeLanguagePreferenceStore { LanguageCode = "de" };
        var service = new LanguageService(preferences);
        service.Initialize(CultureInfo.GetCultureInfo("en-US"));
        var themeService = new ThemeService(new FakeThemePreferenceStore { ThemeCode = "dark" });
        themeService.Initialize();

        var viewModel = new SettingsViewModel(service, themeService, CreateDisplayPreferences());

        Assert.Equal(
            ["en", "es", "de", "fr", "pt-BR", "it", "ja", "ko", "zh-CN", "ar", "hi"],
            viewModel.Languages.Select(language => language.Code));
        Assert.Equal("de", viewModel.SelectedLanguage!.Code);
        Assert.Equal(["light", "dark"], viewModel.Themes.Select(theme => theme.Code));
        Assert.Equal("dark", viewModel.SelectedTheme!.Code);
    }

    [Fact]
    public void Selecting_a_language_applies_and_persists_it()
    {
        var preferences = new FakeLanguagePreferenceStore();
        var service = new LanguageService(preferences);
        service.Initialize(CultureInfo.GetCultureInfo("en-US"));
        var themeService = new ThemeService(new FakeThemePreferenceStore());
        themeService.Initialize();
        var viewModel = new SettingsViewModel(service, themeService, CreateDisplayPreferences());

        viewModel.SelectedLanguage = viewModel.Languages.Single(language => language.Code == "es");

        Assert.Equal("es", service.CurrentLanguageCode);
        Assert.Equal("es", preferences.LanguageCode);
    }

    [Fact]
    public void Language_names_follow_the_active_application_language()
    {
        var service = new LanguageService(new FakeLanguagePreferenceStore());
        service.Initialize(CultureInfo.GetCultureInfo("en-US"));
        var themeService = new ThemeService(new FakeThemePreferenceStore());
        themeService.Initialize();
        var viewModel = new SettingsViewModel(service, themeService, CreateDisplayPreferences());
        var english = viewModel.Languages.Single(language => language.Code == "en");
        var spanish = viewModel.Languages.Single(language => language.Code == "es");

        Assert.Equal("English", viewModel.GetLanguageDisplayName(english));
        viewModel.SelectedLanguage = spanish;

        Assert.Equal("Inglés", viewModel.GetLanguageDisplayName(english));
        Assert.Equal("Español", viewModel.SelectedLanguageDisplayName);
    }

    [Fact]
    public void Selecting_a_theme_applies_and_persists_it()
    {
        var languageService = new LanguageService(new FakeLanguagePreferenceStore());
        languageService.Initialize(CultureInfo.GetCultureInfo("en-US"));
        var preferences = new FakeThemePreferenceStore();
        var themeService = new ThemeService(preferences);
        themeService.Initialize();
        var viewModel = new SettingsViewModel(languageService, themeService, CreateDisplayPreferences());

        viewModel.SelectedTheme = viewModel.Themes.Single(theme => theme.Code == "dark");

        Assert.Equal("dark", themeService.CurrentThemeCode);
        Assert.Equal("dark", preferences.ThemeCode);
    }

    [Fact]
    public void Selector_commands_apply_the_selected_preference()
    {
        var languageStore = new FakeLanguagePreferenceStore();
        var languageService = new LanguageService(languageStore);
        languageService.Initialize(CultureInfo.GetCultureInfo("en-US"));
        var themeService = new ThemeService(new FakeThemePreferenceStore());
        themeService.Initialize();
        var displayPreferences = CreateDisplayPreferences();
        var viewModel = new SettingsViewModel(languageService, themeService, displayPreferences);

        viewModel.SelectLanguageCommand.Execute("de");
        viewModel.SelectThemeCommand.Execute("dark");
        viewModel.SelectDateFormatCommand.Execute(DisplayPreferencesService.MonthDayYearCode);
        viewModel.SelectMeasurementSystemCommand.Execute(DisplayPreferencesService.ImperialCode);
        viewModel.SelectInactivityIntervalCommand.Execute("14");

        Assert.Equal("de", viewModel.SelectedLanguage!.Code);
        Assert.Equal("dark", viewModel.SelectedTheme!.Code);
        Assert.Equal(DisplayPreferencesService.MonthDayYearCode, viewModel.SelectedDateFormat!.Code);
        Assert.Equal(DisplayPreferencesService.ImperialCode, viewModel.SelectedMeasurementSystem!.Code);
        Assert.Equal(14, viewModel.SelectedInactivityInterval!.Days);
    }

    [Fact]
    public void Exposes_and_persists_date_and_measurement_system_preferences()
    {
        var languageService = new LanguageService(new FakeLanguagePreferenceStore());
        languageService.Initialize(CultureInfo.GetCultureInfo("en-US"));
        var displayStore = new FakeDisplayPreferenceStore
        {
            DateFormatCode = DisplayPreferencesService.MonthDayYearCode,
            MeasurementSystemCode = DisplayPreferencesService.ImperialCode
        };
        var displayPreferences = new DisplayPreferencesService(displayStore);
        displayPreferences.Initialize();
        var themeService = new ThemeService(new FakeThemePreferenceStore());
        themeService.Initialize();

        var viewModel = new SettingsViewModel(languageService, themeService, displayPreferences);

        Assert.Equal(["dd/MM/yyyy", "MM/dd/yyyy"], viewModel.DateFormats.Select(option => option.Code));
        Assert.Equal(["metric", "imperial"], viewModel.MeasurementSystems.Select(option => option.Code));
        Assert.Equal("MM/dd/yyyy", viewModel.SelectedDateFormat!.Code);
        Assert.Equal("imperial", viewModel.SelectedMeasurementSystem!.Code);

        viewModel.SelectedDateFormat = viewModel.DateFormats[0];
        viewModel.SelectedMeasurementSystem = viewModel.MeasurementSystems[0];

        Assert.Equal("dd/MM/yyyy", displayStore.DateFormatCode);
        Assert.Equal("metric", displayStore.MeasurementSystemCode);
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

        public string? MeasurementSystemCode { get; set; }

        public string? GetDateFormatCode() => DateFormatCode;

        public string? GetWeightUnitCode() => WeightUnitCode;

        public string? GetHeightUnitCode() => HeightUnitCode;

        public string? GetMeasurementSystemCode() => MeasurementSystemCode;

        public void SetDateFormatCode(string code) => DateFormatCode = code;

        public void SetWeightUnitCode(string code) => WeightUnitCode = code;

        public void SetHeightUnitCode(string code) => HeightUnitCode = code;

        public void SetMeasurementSystemCode(string code) => MeasurementSystemCode = code;
    }
}
