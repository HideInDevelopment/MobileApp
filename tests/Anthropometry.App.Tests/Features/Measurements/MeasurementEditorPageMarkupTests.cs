namespace Anthropometry.App.Tests.Features.Measurements;

public sealed class MeasurementEditorPageMarkupTests
{
    [Fact]
    public void Size_inputs_use_a_decimal_friendly_keyboard()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Features", "Measurements", "MeasurementEditorPage.xaml"));
        var markup = File.ReadAllText(path);

        Assert.Contains("Text=\"{Binding NeckText}\" Keyboard=\"Numeric\"", markup);
        Assert.Contains("Text=\"{Binding AbdomenText}\" Keyboard=\"Numeric\"", markup);
        Assert.Contains("Text=\"{Binding HipText}\" Keyboard=\"Numeric\"", markup);
    }

    [Fact]
    public void Guidance_buttons_cover_weight_and_female_size_topics_with_accessible_touch_targets()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Features", "Measurements", "MeasurementEditorPage.xaml"));
        var markup = File.ReadAllText(path);

        Assert.Contains("CommandParameter=\"{x:Static help:GuidanceTopic.Weight}\"", markup);
        Assert.Contains("CommandParameter=\"{x:Static help:GuidanceTopic.Neck}\"", markup);
        Assert.Contains("CommandParameter=\"{Binding TrunkGuidanceTopic}\"", markup);
        Assert.Contains("CommandParameter=\"{x:Static help:GuidanceTopic.Hip}\"", markup);
        Assert.Contains("SemanticProperties.Description=\"{DynamicResource GuidanceHipTitle}\"", markup);
        Assert.True(markup.Split("MinimumHeightRequest=\"48\"").Length - 1 >= 5);
    }
}
