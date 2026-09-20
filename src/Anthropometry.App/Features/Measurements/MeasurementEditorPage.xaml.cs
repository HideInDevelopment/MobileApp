namespace Anthropometry.App.Features.Measurements;

public partial class MeasurementEditorPage : ContentPage
{
    private bool _entitlementsLoaded;

    public MeasurementEditorPage(MeasurementEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnAppearing(object? sender, EventArgs e)
    {
        if (_entitlementsLoaded || BindingContext is not MeasurementEditorViewModel viewModel)
        {
            return;
        }

        _entitlementsLoaded = true;
        await viewModel.LoadEntitlementsAsync();
    }
}
