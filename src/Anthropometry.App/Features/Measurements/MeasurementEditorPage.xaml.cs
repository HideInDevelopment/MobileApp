namespace Anthropometry.App.Features.Measurements;

public partial class MeasurementEditorPage : ContentPage
{
    public MeasurementEditorPage(MeasurementEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
