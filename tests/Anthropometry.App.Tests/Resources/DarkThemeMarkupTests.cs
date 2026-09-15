using System.Xml.Linq;

namespace Anthropometry.App.Tests.Resources;

public sealed class DarkThemeMarkupTests
{
    [Fact]
    public void Dark_palette_uses_neutral_surface_and_text_colors()
    {
        var colors = ReadColors();

        Assert.Equal("#E5E7EB", colors["PrimaryDark"]);
        Assert.Equal("#111827", colors["PrimaryDarkText"]);
        Assert.Equal("#F9FAFB", colors["SecondaryDarkText"]);
        Assert.Equal("#0F1115", colors["OffBlack"]);
        Assert.DoesNotContain("#AC99EA", colors.Values);
        Assert.DoesNotContain("#9880E5", colors.Values);
    }

    [Fact]
    public void Shared_styles_define_a_dark_page_and_shell_surface()
    {
        var markup = ReadFile(Path.Combine("Resources", "Styles", "Styles.xaml"));

        Assert.Contains("BackgroundColor\" Value=\"{AppThemeBinding Light={StaticResource White}, Dark={StaticResource OffBlack}}", markup);
        Assert.Contains("Shell.BackgroundColor\" Value=\"{AppThemeBinding Light={StaticResource White}, Dark={StaticResource OffBlack}}", markup);
        Assert.Contains("Shell.TitleColor\" Value=\"{AppThemeBinding Light={StaticResource Black}, Dark={StaticResource SecondaryDarkText}}", markup);
    }

    [Fact]
    public void Settings_page_exposes_the_persisted_theme_selection()
    {
        var markup = ReadFile(Path.Combine("Features", "Settings", "SettingsPage.xaml"));

        Assert.Contains("{DynamicResource Theme}", markup);
        Assert.Contains("ItemsSource=\"{Binding Themes}\"", markup);
        Assert.Contains("SelectedItem=\"{Binding SelectedTheme}\"", markup);
    }

    [Fact]
    public void Toolbar_pages_use_high_contrast_dark_icon_assets()
    {
        var profileList = ReadFile(Path.Combine("Features", "Profiles", "ProfileListPage.xaml"));
        var history = ReadFile(Path.Combine("Features", "Measurements", "MeasurementHistoryPage.xaml"));

        Assert.Contains("IconImageSource=\"{AppThemeBinding Light=settings.svg, Dark=settings_dark.svg}\"", profileList);
        Assert.Contains("IconImageSource=\"{AppThemeBinding Light=help.svg, Dark=help_dark.svg}\"", profileList);
        Assert.Contains("IconImageSource=\"{AppThemeBinding Light=ruler.svg, Dark=ruler_dark.svg}\"", history);
    }

    private static Dictionary<string, string> ReadColors()
    {
        var document = XDocument.Parse(ReadFile(Path.Combine("Resources", "Styles", "Colors.xaml")));

        return document
            .Descendants()
            .Where(element => element.Name.LocalName == "Color")
            .ToDictionary(
                element => (string)element.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2009/xaml"))!,
                element => element.Value.Trim(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static string ReadFile(string relativePath)
        => File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", relativePath)));
}
