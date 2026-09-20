using System.Xml.Linq;

namespace Anthropometry.App.Tests.Resources;

public sealed class BrandAssetMarkupTests
{
    [Fact]
    public void Toolbar_icons_use_distinct_neutral_colors()
    {
        Assert.Equal("#374151", ReadPathFill("settings.svg"));
        Assert.Equal("#6B7280", ReadPathFill("help.svg"));
        Assert.Equal("#1F2937", ReadPathFill("ruler.svg"));
    }

    [Fact]
    public void Startup_assets_do_not_use_the_default_dotnet_wordmark()
    {
        var splash = ReadAsset("Splash", "splash.svg");
        var appIconPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Resources", "AppIcon", "appicon.png"));

        Assert.DoesNotContain("105.50037", splash);
        Assert.Contains("fill=\"#FFFFFF\"", splash);
        Assert.True(File.Exists(appIconPath));
        Assert.True(new FileInfo(appIconPath).Length > 100_000);
    }

    private static string ReadPathFill(string fileName)
    {
        var document = XDocument.Parse(ReadAsset("Images", fileName));
        var path = document.Descendants().Single(element => element.Name.LocalName == "path");
        return (string)path.Attribute("fill")!;
    }

    private static string ReadAsset(string folder, string fileName)
        => File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Resources", folder, fileName)));
}
