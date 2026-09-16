using System.Xml.Linq;

namespace Anthropometry.App.Tests.Resources;

public sealed class LightThemeMarkupTests
{
    [Fact]
    public void Light_palette_uses_charcoal_accents_and_white_surface()
    {
        var colors = ReadColors();

        Assert.Equal("#1F2937", colors["Primary"]);
        Assert.Equal("#111827", colors["Tertiary"]);
        Assert.Equal("#E5E7EB", colors["Secondary"]);
        Assert.Equal("#FFFFFF", colors["White"]);
        Assert.Equal("#1F2937", colors["MidnightBlue"]);
        Assert.DoesNotContain("#512BD4", colors.Values);
        Assert.DoesNotContain("#D600AA", colors.Values);
    }

    [Fact]
    public void Shared_styles_define_semantic_action_and_card_styles()
    {
        var markup = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Resources", "Styles", "Styles.xaml")));

        Assert.Contains("x:Key=\"PrimaryButton\"", markup);
        Assert.Contains("x:Key=\"SecondaryButton\"", markup);
        Assert.Contains("x:Key=\"DestructiveButton\"", markup);
        Assert.Contains("x:Key=\"CardBorder\"", markup);
        Assert.Contains("MinimumHeightRequest\" Value=\"48\"", markup);

        var primaryButtonStart = markup.IndexOf("x:Key=\"PrimaryButton\"", StringComparison.Ordinal);
        var primaryButtonEnd = markup.IndexOf("</Style>", primaryButtonStart, StringComparison.Ordinal);
        var primaryButtonStyle = markup[primaryButtonStart..primaryButtonEnd];
        Assert.Contains("x:Name=\"Disabled\"", primaryButtonStyle);
        Assert.Contains("Opacity", primaryButtonStyle);
    }

    private static Dictionary<string, string> ReadColors()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Resources", "Styles", "Colors.xaml"));

        return XDocument.Load(path)
            .Descendants()
            .Where(element => element.Name.LocalName == "Color")
            .ToDictionary(
                element => (string)element.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2009/xaml"))!,
                element => element.Value.Trim(),
                StringComparer.Ordinal);
    }
}
