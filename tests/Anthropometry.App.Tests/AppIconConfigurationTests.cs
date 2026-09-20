namespace Anthropometry.App.Tests;

public sealed class AppIconConfigurationTests
{
    [Fact]
    public void Uses_the_official_png_logo_as_the_android_app_icon()
    {
        var projectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Anthropometry.App.csproj"));
        var iconPath = Path.Combine(
            Path.GetDirectoryName(projectPath)!,
            "Resources", "AppIcon", "appicon.png");
        var project = File.ReadAllText(projectPath);

        Assert.Contains("<MauiIcon Include=\"Resources\\AppIcon\\appicon.png\" />", project);
        Assert.DoesNotContain("ForegroundFile=", project);
        Assert.True(File.Exists(iconPath));
    }
}
