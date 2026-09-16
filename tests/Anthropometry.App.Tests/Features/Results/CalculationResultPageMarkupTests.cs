namespace Anthropometry.App.Tests.Features.Results;

public sealed class CalculationResultPageMarkupTests
{
    [Fact]
    public void Shows_disclaimer_and_descriptions_without_redundant_history_action()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Features", "Results", "CalculationResultPage.xaml"));
        var markup = File.ReadAllText(path);

        Assert.Contains("Text=\"{DynamicResource ResultsEstimateDisclaimer}\"", markup);
        Assert.Contains("Text=\"{Binding Description}\"", markup);
        Assert.DoesNotContain("Text=\"{DynamicResource ViewHistory}\"", markup);
        Assert.DoesNotContain("Command=\"{Binding ViewHistoryCommand}\"", markup);
    }
}
