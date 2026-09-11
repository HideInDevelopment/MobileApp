namespace Anthropometry.App.Features.Measurements;

public partial class WeightGraphicPage : ContentPage
{
    private readonly WeightGraphicViewModel _viewModel;

    public WeightGraphicPage(WeightGraphicViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
        WeightChart.Drawable = new WeightGraphicDrawable(
            _viewModel.Points,
            _viewModel.DateAxisLabel,
            _viewModel.WeightAxisLabel,
            (float)_viewModel.ChartMinimumWeight,
            (float)_viewModel.ChartMaximumWeight);
        WeightChart.Invalidate();
    }

    private void OnChartStartInteraction(object? sender, TouchEventArgs e)
    {
        if (e.Touches.Length == 0 || WeightChart.Drawable is not WeightGraphicDrawable drawable)
        {
            return;
        }

        _viewModel.SelectPoint(drawable.FindNearestPoint(
            e.Touches[0],
            (float)WeightChart.Width,
            (float)WeightChart.Height));
    }
}
