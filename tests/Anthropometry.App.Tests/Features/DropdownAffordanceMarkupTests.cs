namespace Anthropometry.App.Tests.Features;

public sealed class DropdownAffordanceMarkupTests
{
    [Fact]
    public void Every_picker_has_a_visible_down_arrow_that_does_not_block_taps()
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

        var pickerCount = markup.Split("<Picker").Length - 1;
        var arrowCount = markup.Split("Style=\"{StaticResource DropdownArrow}\"").Length - 1;

        Assert.Equal(9, pickerCount);
        Assert.Equal(pickerCount, arrowCount);
    }
}
