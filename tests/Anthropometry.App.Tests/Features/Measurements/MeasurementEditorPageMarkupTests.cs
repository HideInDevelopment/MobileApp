namespace Anthropometry.App.Tests.Features.Measurements;

public sealed class MeasurementEditorPageMarkupTests
{
    [Fact]
    public void Size_inputs_use_a_decimal_friendly_keyboard()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Features", "Measurements", "MeasurementEditorPage.xaml"));
        var markup = File.ReadAllText(path);

        Assert.Contains("Text=\"{Binding NeckText}\" Keyboard=\"Text\"", markup);
        Assert.Contains("Text=\"{Binding AbdomenText}\" Keyboard=\"Text\"", markup);
        Assert.Contains("Text=\"{Binding HipText}\" Keyboard=\"Text\"", markup);
    }
}
