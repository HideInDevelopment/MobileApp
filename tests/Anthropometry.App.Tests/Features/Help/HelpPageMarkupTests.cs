namespace Anthropometry.App.Tests.Features.Help;

public sealed class HelpPageMarkupTests
{
    [Fact]
    public void Shows_the_total_daily_energy_expenditure_equation()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Features", "Help", "HelpPage.xaml"));
        var markup = File.ReadAllText(path);

        Assert.Contains("Text=\"{DynamicResource HelpTdeeFormula}\"", markup);
    }
}
