namespace Anthropometry.App.Features.Measurements;

public partial class MeasurementHistoryPage : ContentPage
{
    private readonly MeasurementHistoryViewModel _viewModel;

    public MeasurementHistoryPage(MeasurementHistoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
