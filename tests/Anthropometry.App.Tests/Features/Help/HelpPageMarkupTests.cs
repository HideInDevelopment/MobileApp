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

    [Fact]
    public void Provides_a_second_help_page_for_profile_transfer_codes()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Features", "Help", "HelpPage.xaml"));
        var markup = File.ReadAllText(path);

        Assert.Contains("x:Name=\"TransferPage\"", markup);
        Assert.Contains("Text=\"{DynamicResource HelpTransferTitle}\"", markup);
        Assert.Contains("Text=\"{DynamicResource HelpTransferCodeBody}\"", markup);
        Assert.Contains("Clicked=\"OnNextClicked\"", markup);
        Assert.Contains("Clicked=\"OnPreviousClicked\"", markup);
        Assert.DoesNotContain("CSV", markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Places_reminder_guidance_between_equations_and_transfer_pages()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Features", "Help", "HelpPage.xaml"));
        var markup = File.ReadAllText(path);

        var equationsIndex = markup.IndexOf("x:Name=\"EquationsPage\"", StringComparison.Ordinal);
        var remindersIndex = markup.IndexOf("x:Name=\"ReminderPage\"", StringComparison.Ordinal);
        var transferIndex = markup.IndexOf("x:Name=\"TransferPage\"", StringComparison.Ordinal);

        Assert.True(equationsIndex >= 0);
        Assert.True(remindersIndex > equationsIndex);
        Assert.True(transferIndex > remindersIndex);
        Assert.Contains("Text=\"{DynamicResource HelpRemindersTitle}\"", markup);
        Assert.Contains("Text=\"1 / 4\"", markup);
    }
}
