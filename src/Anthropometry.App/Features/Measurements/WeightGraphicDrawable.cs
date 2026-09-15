using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Anthropometry.App.Features.Measurements;

public sealed class WeightGraphicDrawable : IDrawable
{
    private const int TickCount = 4;
    private const float PlotLeft = 58;
    private const float PlotRight = 18;
    private const float PlotTop = 28;
    private const float PlotBottomPadding = 64;
    private const float DateLabelWidth = 40;
    private const float PointHitRadius = 24;
    private readonly IReadOnlyList<WeightGraphicPoint> _points;
    private readonly string _dateAxisLabel;
    private readonly string _weightAxisLabel;
    private readonly float _minimumWeight;
    private readonly float _maximumWeight;

    public WeightGraphicDrawable(
        IReadOnlyList<WeightGraphicPoint> points,
        string dateAxisLabel,
        string weightAxisLabel,
        float minimumWeight,
        float maximumWeight)
    {
        _points = points;
        _dateAxisLabel = dateAxisLabel;
        _weightAxisLabel = weightAxisLabel;
        _minimumWeight = minimumWeight;
        _maximumWeight = maximumWeight;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (_points.Count == 0 || dirtyRect.Width <= 0 || dirtyRect.Height <= 0)
        {
            return;
        }

        var plotBottom = dirtyRect.Height - PlotBottomPadding;
        var plotWidth = MathF.Max(1, dirtyRect.Width - PlotLeft - PlotRight);
        var plotHeight = MathF.Max(1, plotBottom - PlotTop);
        var minWeight = _minimumWeight;
        var weightRange = _maximumWeight - minWeight;
        var isDarkTheme = Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark;
        var chartTextColor = isDarkTheme ? Color.FromArgb("#D1D5DB") : Color.FromArgb("#4B5563");
        var chartGridColor = isDarkTheme ? Color.FromArgb("#374151") : Color.FromArgb("#D1D5DB");
        var chartAxisColor = isDarkTheme ? Color.FromArgb("#9CA3AF") : Color.FromArgb("#6B7280");
        var chartAccentColor = isDarkTheme ? Color.FromArgb("#E5E7EB") : Color.FromArgb("#1F2937");

        canvas.SaveState();
        canvas.FontColor = chartTextColor;
        canvas.FontSize = 12;
        canvas.StrokeColor = chartGridColor;
        canvas.StrokeSize = 1;

        for (var tick = 0; tick <= TickCount; tick++)
        {
            var ratio = (float)tick / TickCount;
            var y = plotBottom - plotHeight * ratio;
            canvas.DrawLine(PlotLeft, y, dirtyRect.Width - PlotRight, y);
            var value = minWeight + weightRange * ratio;
            canvas.DrawString(
                value.ToString("0.#", CultureInfo.CurrentCulture),
                0,
                y - 12,
                PlotLeft - 8,
                24,
                HorizontalAlignment.Right,
                VerticalAlignment.Center);
        }

        canvas.StrokeColor = chartAxisColor;
        canvas.StrokeSize = 1.5f;
        canvas.DrawLine(PlotLeft, PlotTop, PlotLeft, plotBottom);
        canvas.DrawLine(PlotLeft, plotBottom, dirtyRect.Width - PlotRight, plotBottom);

        var path = new PathF();
        for (var index = 0; index < _points.Count; index++)
        {
            var point = GetPointPosition(index, dirtyRect.Width, dirtyRect.Height);
            var x = point.X;
            var y = point.Y;
            if (index == 0)
            {
                path.MoveTo(x, y);
            }
            else
            {
                path.LineTo(x, y);
            }
        }

        canvas.StrokeColor = chartAccentColor;
        canvas.StrokeSize = 2.5f;
        canvas.DrawPath(path);
        canvas.FillColor = chartAccentColor;
        for (var index = 0; index < _points.Count; index++)
        {
            var point = GetPointPosition(index, dirtyRect.Width, dirtyRect.Height);
            var x = point.X;
            var y = point.Y;
            canvas.FillCircle(x, y, 5);
            canvas.DrawString(
                _points[index].DateText,
                x - DateLabelWidth / 2,
                plotBottom + 8,
                DateLabelWidth,
                20,
                HorizontalAlignment.Center,
                VerticalAlignment.Center);
        }

        canvas.FontSize = 13;
        canvas.DrawString(
            _weightAxisLabel,
            0,
            0,
            PlotLeft,
            22,
            HorizontalAlignment.Center,
            VerticalAlignment.Center);
        canvas.DrawString(
            _dateAxisLabel,
            PlotLeft,
            dirtyRect.Height - 28,
            plotWidth,
            22,
            HorizontalAlignment.Center,
            VerticalAlignment.Center);
        canvas.RestoreState();
    }

    public WeightGraphicPoint? FindNearestPoint(PointF touch, float chartWidth, float chartHeight)
    {
        if (_points.Count == 0 || chartWidth <= 0 || chartHeight <= 0)
        {
            return null;
        }

        var nearestIndex = -1;
        var nearestDistanceSquared = PointHitRadius * PointHitRadius;
        for (var index = 0; index < _points.Count; index++)
        {
            var point = GetPointPosition(index, chartWidth, chartHeight);
            var deltaX = touch.X - point.X;
            var deltaY = touch.Y - point.Y;
            var distanceSquared = deltaX * deltaX + deltaY * deltaY;
            if (distanceSquared <= nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearestIndex = index;
            }
        }

        return nearestIndex >= 0 ? _points[nearestIndex] : null;
    }

    public PointF GetPointPosition(int index, float chartWidth, float chartHeight)
    {
        var plotBottom = chartHeight - PlotBottomPadding;
        var plotWidth = MathF.Max(1, chartWidth - PlotLeft - PlotRight);
        var plotHeight = MathF.Max(1, plotBottom - PlotTop);
        var x = _points.Count == 1
            ? PlotLeft + plotWidth / 2
            : PlotLeft + plotWidth * index / (_points.Count - 1);
        var y = plotBottom - ((float)_points[index].WeightKg - _minimumWeight) / (_maximumWeight - _minimumWeight) * plotHeight;
        return new PointF(x, y);
    }
}
