namespace Anthropometry.App.Tests.Features.Measurements;

public sealed class MeasurementHistoryPageMarkupTests
{
    [Fact]
    public void Exposes_edit_and_delete_actions_for_each_history_item()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Features", "Measurements", "MeasurementHistoryPage.xaml"));
        var markup = File.ReadAllText(path);

        Assert.Contains("Text=\"{DynamicResource EditMeasurement}\"", markup);
        Assert.Contains("Text=\"{DynamicResource DeleteMeasurement}\"", markup);
        Assert.Contains("Path=BindingContext.EditCommand", markup);
        Assert.Contains("Path=BindingContext.DeleteCommand", markup);
    }

    [Fact]
    public void Exposes_chart_options_as_a_dismissible_dropdown_menu()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Features", "Measurements", "MeasurementHistoryPage.xaml"));
        var markup = File.ReadAllText(path);

        Assert.Contains("IsVisible=\"{Binding IsChartMenuVisible}\"", markup);
        Assert.Contains("Text=\"{DynamicResource MetricChart}\"", markup);
        Assert.Contains("Command=\"{Binding WeightGraphicCommand}\"", markup);
        Assert.Contains("Command=\"{Binding DismissChartMenuCommand}\"", markup);
    }
}
