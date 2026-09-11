namespace Anthropometry.App.Tests.Features;

public sealed class DuplicatePageTitleMarkupTests
{
    [Fact]
    public void Profile_detail_does_not_repeat_the_profile_name()
        => AssertPageDoesNotContain(
            "Profiles", "ProfileDetailPage.xaml",
            "<Label Text=\"{Binding Profile.Name}\" FontSize=\"28\" FontAttributes=\"Bold\" />");

    [Fact]
    public void Profile_editor_does_not_repeat_the_page_title()
        => AssertPageDoesNotContain(
            "Profiles", "ProfileEditorPage.xaml",
            "<Label Text=\"{Binding Title}\" FontSize=\"28\" FontAttributes=\"Bold\" />");

    [Fact]
    public void Measurement_editor_does_not_repeat_the_page_title()
        => AssertPageDoesNotContain(
            "Measurements", "MeasurementEditorPage.xaml",
            "<Label Text=\"{Binding Title}\" FontSize=\"28\" FontAttributes=\"Bold\" />");

    [Fact]
    public void Settings_does_not_repeat_the_page_title()
        => AssertPageDoesNotContain(
            "Settings", "SettingsPage.xaml",
            "<Label Text=\"{DynamicResource SettingsTitle}\" FontSize=\"28\" FontAttributes=\"Bold\" />");

    [Fact]
    public void Measurement_history_does_not_repeat_the_page_title()
        => AssertPageDoesNotContain(
            "Measurements", "MeasurementHistoryPage.xaml",
            "<Label Grid.Row=\"0\" Text=\"{DynamicResource MeasurementHistory}\" FontSize=\"28\" FontAttributes=\"Bold\" />");

    [Fact]
    public void Weight_graphic_does_not_repeat_the_page_title()
    {
        var markup = ReadPage("Measurements", "WeightGraphicPage.xaml");

        Assert.DoesNotContain("Text=\"{DynamicResource WeightGraphicTitle}\"", markup);
    }

    private static void AssertPageDoesNotContain(string feature, string fileName, string duplicateMarkup)
    {
        var markup = ReadPage(feature, fileName);

        Assert.DoesNotContain(duplicateMarkup, markup);
    }

    private static string ReadPage(string feature, string fileName)
        => File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Features", feature, fileName)));
}
