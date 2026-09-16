using Anthropometry.App.Platforms.Android;

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

    private void OnMeasurementTypeSelectorClicked(object? sender, EventArgs e)
    {
        if (sender is View view && BindingContext is MeasurementHistoryViewModel viewModel)
        {
            ContextMenuHelper.Show(
                view,
                viewModel.MeasurementTypeOptions
                    .Select(option => new ContextMenuOption(
                        option.DisplayName,
                        () => viewModel.SelectMeasurementTypeCommand.Execute(option.Value?.ToString() ?? string.Empty)))
                    .ToArray());
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
