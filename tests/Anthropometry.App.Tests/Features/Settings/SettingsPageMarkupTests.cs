namespace Anthropometry.App.Tests.Features.Settings;

public sealed class SettingsPageMarkupTests
{
    [Fact]
    public void Exposes_context_menu_selectors_for_persisted_preferences()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Features", "Settings", "SettingsPage.xaml"));
        var markup = File.ReadAllText(path);

        Assert.DoesNotContain("<Picker", markup);
        Assert.Contains("OnLanguageSelectorClicked", markup);
        Assert.Contains("OnThemeSelectorClicked", markup);
        Assert.Contains("OnDateFormatSelectorClicked", markup);
        Assert.Contains("OnMeasurementSystemSelectorClicked", markup);
        Assert.Contains("OnInactivityIntervalSelectorClicked", markup);
        Assert.Contains("{DynamicResource PremiumTitle}", markup, StringComparison.Ordinal);
        Assert.Contains("{Binding PremiumStatusText}", markup, StringComparison.Ordinal);
        Assert.Contains("{Binding PremiumCommand}", markup, StringComparison.Ordinal);
    }
}
