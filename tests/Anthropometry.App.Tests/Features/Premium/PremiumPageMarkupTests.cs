namespace Anthropometry.App.Tests.Features.Premium;

public sealed class PremiumPageMarkupTests
{
    [Fact]
    public void Premium_page_exposes_status_purchase_restore_and_manage_actions()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "src", "Anthropometry.App", "Features", "Premium", "PremiumPage.xaml");
        var markup = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("{Binding StateText}", markup, StringComparison.Ordinal);
        Assert.Contains("{Binding SubscribeCommand}", markup, StringComparison.Ordinal);
        Assert.Contains("{Binding RestoreCommand}", markup, StringComparison.Ordinal);
        Assert.Contains("{Binding ManageSubscriptionCommand}", markup, StringComparison.Ordinal);
    }
}
