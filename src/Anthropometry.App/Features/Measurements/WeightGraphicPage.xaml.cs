using Microsoft.Maui.Graphics;
using System.ComponentModel;

namespace Anthropometry.App.Features.Measurements;

public partial class WeightGraphicPage : ContentPage
{
    private readonly WeightGraphicViewModel _viewModel;
    private WeightGraphicPoint? _selectedPoint;

    public WeightGraphicPage(WeightGraphicViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
        UpdateChartDrawable();
        _selectedPoint = null;
        PositionLegendBubble();
        WeightChart.Invalidate();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(WeightGraphicViewModel.ChartMinimumValue)
            or nameof(WeightGraphicViewModel.ChartMaximumValue)
            or nameof(WeightGraphicViewModel.MetricAxisLabel)
            or nameof(WeightGraphicViewModel.ValueAxisLabel)
            or nameof(WeightGraphicViewModel.WeightAxisLabel)
            or nameof(WeightGraphicViewModel.DateAxisLabel))
        {
            UpdateChartDrawable();
            WeightChart.Invalidate();
        }
    }

    private void UpdateChartDrawable()
    {
        WeightChart.Drawable = new WeightGraphicDrawable(
            _viewModel.Points,
            _viewModel.DateAxisLabel,
            _viewModel.MetricAxisLabel,
            (float)_viewModel.ChartMinimumValue,
            (float)_viewModel.ChartMaximumValue);
    }

    private void OnChartStartInteraction(object? sender, TouchEventArgs e)
    {
        if (e.Touches.Length == 0 || WeightChart.Drawable is not WeightGraphicDrawable drawable)
        {
            return;
        }

        _selectedPoint = drawable.FindNearestPoint(
            e.Touches[0],
            (float)WeightChart.Width,
            (float)WeightChart.Height);
        _viewModel.SelectPoint(_selectedPoint);
        PositionLegendBubble();
    }

    private void OnLegendBubbleSizeChanged(object? sender, EventArgs e)
    {
        PositionLegendBubble();
    }

    private void PositionLegendBubble()
    {
        if (_selectedPoint is null || WeightChart.Drawable is not WeightGraphicDrawable drawable)
        {
            return;
        }

        var pointIndex = _viewModel.Points.IndexOf(_selectedPoint);
        if (pointIndex < 0 || LegendBubble.Width <= 0 || LegendBubble.Height <= 0)
        {
            return;
        }

        var point = drawable.GetPointPosition(
            pointIndex,
            (float)WeightChart.Width,
            (float)WeightChart.Height);
        var x = point.X - (float)LegendBubble.Width / 2;
        var y = point.Y - (float)LegendBubble.Height - 12;
        if (y < 0)
        {
            y = point.Y + 12;
        }

        var maxX = MathF.Max(0, (float)ChartContainer.Width - (float)LegendBubble.Width);
        var maxY = MathF.Max(0, (float)ChartContainer.Height - (float)LegendBubble.Height);
        AbsoluteLayout.SetLayoutBounds(
            LegendBubble,
            new Rect(Math.Clamp(x, 0, maxX), Math.Clamp(y, 0, maxY), -1, -1));
    }
}
