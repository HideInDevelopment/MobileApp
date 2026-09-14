namespace Anthropometry.App.Tests.Features.Profiles;

public sealed class ProfileGenderMarkupTests
{
    [Fact]
    public void Profile_row_places_a_gender_icon_before_the_profile_name()
    {
        var markup = ReadMarkup("ProfileRowView.xaml");

        Assert.Contains("GenderIconConverter", markup);
        Assert.Contains("Grid.Column=\"0\"", markup);
        Assert.Contains("Grid.Column=\"1\"", markup);
        Assert.Contains("Text=\"{Binding Name}\"", markup);
        Assert.True(
            markup.IndexOf("Gender, Converter", StringComparison.Ordinal)
                < markup.IndexOf("Text=\"{Binding Name}\"", StringComparison.Ordinal));
    }

    [Fact]
    public void Profile_editor_exposes_a_gender_picker()
    {
        var markup = ReadMarkup("ProfileEditorPage.xaml");

        Assert.Contains("{DynamicResource Gender}", markup);
        Assert.Contains("ItemsSource=\"{Binding GenderOptions}\"", markup);
        Assert.Contains("SelectedItem=\"{Binding SelectedGender}\"", markup);
    }

    private static string ReadMarkup(string fileName)
        => File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src",
            "Anthropometry.App",
            "Features",
            "Profiles",
            fileName)));
}
