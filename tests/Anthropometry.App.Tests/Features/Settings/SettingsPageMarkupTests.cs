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
        Assert.Contains("ItemsSource=\"{Binding MeasurementSystems}\"", markup);
        Assert.Contains("SelectedItem=\"{Binding SelectedMeasurementSystem}\"", markup);
        Assert.DoesNotContain("ItemsSource=\"{Binding WeightUnits}\"", markup);
        Assert.DoesNotContain("ItemsSource=\"{Binding HeightUnits}\"", markup);
    }
}
