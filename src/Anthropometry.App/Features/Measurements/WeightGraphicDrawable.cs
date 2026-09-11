using System.Globalization;
using Microsoft.Maui.Graphics;

namespace Anthropometry.App.Features.Measurements;

public sealed class WeightGraphicDrawable : IDrawable
{
    private const int TickCount = 4;
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

        const float left = 58;
        const float right = 18;
        const float top = 28;
        const float bottom = 64;
        var plotBottom = dirtyRect.Height - bottom;
        var plotWidth = MathF.Max(1, dirtyRect.Width - left - right);
        var plotHeight = MathF.Max(1, plotBottom - top);
        var minWeight = _minimumWeight;
        var weightRange = _maximumWeight - minWeight;

        canvas.SaveState();
        canvas.FontColor = Color.FromArgb("#4B5563");
        canvas.FontSize = 12;
        canvas.StrokeColor = Color.FromArgb("#D1D5DB");
        canvas.StrokeSize = 1;

        for (var tick = 0; tick <= TickCount; tick++)
        {
            var ratio = (float)tick / TickCount;
            var y = plotBottom - plotHeight * ratio;
            canvas.DrawLine(left, y, dirtyRect.Width - right, y);
            var value = minWeight + weightRange * ratio;
            canvas.DrawString(
                value.ToString("0.#", CultureInfo.CurrentCulture),
                0,
                y - 12,
                left - 8,
                24,
                HorizontalAlignment.Right,
                VerticalAlignment.Center);
        }

        canvas.StrokeColor = Color.FromArgb("#6B7280");
        canvas.StrokeSize = 1.5f;
        canvas.DrawLine(left, top, left, plotBottom);
        canvas.DrawLine(left, plotBottom, dirtyRect.Width - right, plotBottom);

        var path = new PathF();
        for (var index = 0; index < _points.Count; index++)
        {
            var x = _points.Count == 1
                ? left + plotWidth / 2
                : left + plotWidth * index / (_points.Count - 1);
            var y = plotBottom - ((float)_points[index].WeightKg - minWeight) / weightRange * plotHeight;
            if (index == 0)
            {
                path.MoveTo(x, y);
            }
            else
            {
                path.LineTo(x, y);
            }
        }

        canvas.StrokeColor = Color.FromArgb("#512BD4");
        canvas.StrokeSize = 2.5f;
        canvas.DrawPath(path);
        canvas.FillColor = Color.FromArgb("#512BD4");
        for (var index = 0; index < _points.Count; index++)
        {
            var x = _points.Count == 1
                ? left + plotWidth / 2
                : left + plotWidth * index / (_points.Count - 1);
            var y = plotBottom - ((float)_points[index].WeightKg - minWeight) / weightRange * plotHeight;
            canvas.FillCircle(x, y, 5);
            canvas.DrawString(
                _points[index].DateText,
                x - 32,
                plotBottom + 8,
                64,
                20,
                HorizontalAlignment.Center,
                VerticalAlignment.Center);
        }

        canvas.FontSize = 13;
        canvas.DrawString(
            _weightAxisLabel,
            0,
            0,
            left,
            22,
            HorizontalAlignment.Center,
            VerticalAlignment.Center);
        canvas.DrawString(
            _dateAxisLabel,
            left,
            dirtyRect.Height - 28,
            plotWidth,
            22,
            HorizontalAlignment.Center,
            VerticalAlignment.Center);
        canvas.RestoreState();
    }
}
