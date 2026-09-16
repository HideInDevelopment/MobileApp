namespace Anthropometry.App.Tests.Features.Measurements;

public sealed class MeasurementHistoryFilterTests
{
    [Fact]
    public void History_page_exposes_local_filters_behind_filter_action()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Features", "Measurements", "MeasurementHistoryPage.xaml"));
        var markup = File.ReadAllText(path);

        Assert.Contains("Text=\"{DynamicResource Filter}\"", markup);
        Assert.Contains("IsVisible=\"{Binding IsFilterPanelVisible}\"", markup);
        Assert.Contains("Date=\"{Binding FromDate}\"", markup);
        Assert.Contains("Date=\"{Binding ToDate}\"", markup);
        Assert.Contains("ItemsSource=\"{Binding MeasurementTypeOptions}\"", markup);
        Assert.Contains("Command=\"{Binding ClearFiltersCommand}\"", markup);
        Assert.Contains("Text=\"{Binding EmptyStateTitle}\"", markup);
    }
}
