namespace Anthropometry.App.Features.Premium;

public partial class PremiumPage : ContentPage
{
    private bool _loaded;

    public PremiumPage(PremiumViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnAppearing(object? sender, EventArgs e)
    {
        if (_loaded || BindingContext is not PremiumViewModel viewModel)
        {
            return;
        }

        _loaded = true;
        await viewModel.LoadCommand.ExecuteAsync(null);
    }
}
