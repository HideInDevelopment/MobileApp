namespace Anthropometry.App.Tests.Features.Profiles;

public sealed class ProfileEditorPageMarkupTests
{
    [Fact]
    public void Activity_level_guidance_button_is_accessible_and_touchable()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Features", "Profiles", "ProfileEditorPage.xaml"));
        var markup = File.ReadAllText(path);

        Assert.Contains("CommandParameter=\"{x:Static help:GuidanceTopic.ActivityLevel}\"", markup);
        Assert.Contains("SemanticProperties.Description=\"{DynamicResource GuidanceActivityLevelTitle}\"", markup);
        Assert.Contains("MinimumHeightRequest=\"48\"", markup);
    }
}
