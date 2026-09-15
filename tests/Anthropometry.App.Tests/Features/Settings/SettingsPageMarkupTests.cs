namespace Anthropometry.App.Tests.Features.Settings;

public sealed class SettingsPageMarkupTests
{
    [Fact]
    public void Exposes_date_and_unit_selectors()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Features", "Settings", "SettingsPage.xaml"));
        var markup = File.ReadAllText(path);

        Assert.Contains("ItemsSource=\"{Binding DateFormats}\"", markup);
        Assert.Contains("SelectedItem=\"{Binding SelectedDateFormat}\"", markup);
        Assert.Contains("ItemsSource=\"{Binding WeightUnits}\"", markup);
        Assert.Contains("SelectedItem=\"{Binding SelectedWeightUnit}\"", markup);
        Assert.Contains("ItemsSource=\"{Binding HeightUnits}\"", markup);
        Assert.Contains("SelectedItem=\"{Binding SelectedHeightUnit}\"", markup);
    }
}
