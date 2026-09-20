namespace Anthropometry.App.Tests.Features.Measurements;

public sealed class MetricChartPageMarkupTests
{
    [Fact]
    public void Chart_page_exposes_metric_and_date_range_controls()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Features", "Measurements", "WeightGraphicPage.xaml"));
        var markup = File.ReadAllText(path);

        Assert.Contains("Title=\"{DynamicResource MetricChartTitle}\"", markup);
        Assert.DoesNotContain("<Picker", markup);
        Assert.Contains("OnMetricSelectorClicked", markup);
        Assert.Contains("Date=\"{Binding FromDate}\"", markup);
        Assert.Contains("Date=\"{Binding ToDate}\"", markup);
        Assert.Contains("Command=\"{Binding ClearDateRangeCommand}\"", markup);
        Assert.Contains("IsVisible=\"{Binding IsFullGraphicsLocked}\"", markup);
        Assert.Contains("{DynamicResource PremiumRequired}", markup);
    }
}
