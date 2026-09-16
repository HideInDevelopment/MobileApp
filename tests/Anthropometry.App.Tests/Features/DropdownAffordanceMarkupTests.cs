namespace Anthropometry.App.Tests.Features;

public sealed class DropdownAffordanceMarkupTests
{
    [Fact]
    public void Every_discrete_selector_has_a_context_menu_and_visible_down_arrow()
    {
        var root = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App"));
        var markup = string.Join(
            Environment.NewLine,
            File.ReadAllText(Path.Combine(root, "Features", "Profiles", "ProfileEditorPage.xaml")),
            File.ReadAllText(Path.Combine(root, "Features", "Settings", "SettingsPage.xaml")),
            File.ReadAllText(Path.Combine(root, "Features", "Measurements", "MeasurementHistoryPage.xaml")),
            File.ReadAllText(Path.Combine(root, "Features", "Measurements", "WeightGraphicPage.xaml")));

        var menuCount = markup.Split("SelectorClicked").Length - 1;
        var arrowCount = markup.Split("Style=\"{StaticResource DropdownArrow}\"").Length - 1;

        Assert.Equal(9, menuCount);
        Assert.Equal(menuCount, arrowCount);
    }
}
