namespace Anthropometry.App.Tests.Features.Results;

public sealed class CalculationResultPageMarkupTests
{
    [Fact]
    public void Shows_disclaimer_descriptions_and_history_action()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Features", "Results", "CalculationResultPage.xaml"));
        var markup = File.ReadAllText(path);

        Assert.Contains("Text=\"{DynamicResource ResultsEstimateDisclaimer}\"", markup);
        Assert.Contains("Text=\"{Binding Description}\"", markup);
        Assert.Contains("Text=\"{DynamicResource ViewHistory}\"", markup);
        Assert.Contains("Command=\"{Binding ViewHistoryCommand}\"", markup);
    }
}
