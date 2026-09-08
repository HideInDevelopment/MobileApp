namespace Anthropometry.App.Features.Results;

public partial class CalculationResultPage : ContentPage
{
    private readonly CalculationResultViewModel _viewModel;

    public CalculationResultPage(CalculationResultViewModel viewModel)
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
