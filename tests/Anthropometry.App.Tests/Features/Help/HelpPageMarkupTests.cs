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

    [Fact]
    public void Does_not_repeat_the_help_title_inside_the_page()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Features", "Help", "HelpPage.xaml"));
        var markup = File.ReadAllText(path);

        Assert.DoesNotContain("<Label Text=\"{DynamicResource HelpTitle}\"", markup);
    }

    [Fact]
    public void Shows_localized_guidance_topics_for_measurements_and_estimates()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Features", "Help", "HelpPage.xaml"));
        var markup = File.ReadAllText(path);

        Assert.Contains("Text=\"{DynamicResource GuidanceWeightBody}\"", markup);
        Assert.Contains("Text=\"{DynamicResource GuidanceNeckBody}\"", markup);
        Assert.Contains("Text=\"{DynamicResource GuidanceEstimateDisclaimerBody}\"", markup);
    }
}
