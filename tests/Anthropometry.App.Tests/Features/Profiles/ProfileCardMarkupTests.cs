namespace Anthropometry.App.Tests.Features.Profiles;

public sealed class ProfileCardMarkupTests
{
    [Fact]
    public void Profile_row_uses_a_shared_card_and_primary_name_style()
    {
        var markup = ReadMarkup("ProfileRowView.xaml");

        Assert.Contains("Style=\"{StaticResource CardBorder}\"", markup);
        Assert.Contains("Style=\"{StaticResource CardTitle}\"", markup);
        Assert.Contains("SemanticProperties.Description=\"{Binding Name}\"", markup);
    }

    [Fact]
    public void Profile_list_distinguishes_secondary_and_destructive_actions()
    {
        var markup = ReadMarkup("ProfileListPage.xaml");

        Assert.Contains("Style=\"{StaticResource SecondaryButton}\"", markup);
        Assert.Contains("Style=\"{StaticResource DestructiveButton}\"", markup);
        Assert.Contains("SemanticProperties.Description=\"{DynamicResource OpenProfile}\"", markup);
        Assert.Contains("SemanticProperties.Description=\"{DynamicResource DeleteProfile}\"", markup);
    }

    private static string ReadMarkup(string fileName)
        => File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Features", "Profiles", fileName)));
}
