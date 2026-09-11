using System.Collections.ObjectModel;
using System.Globalization;
using Anthropometry.Application.Measurements;
using Anthropometry.App.Localization;
using Anthropometry.Domain.Profiles;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anthropometry.App.Features.Measurements;

public sealed record WeightGraphicPoint(
    DateTimeOffset MeasuredAtUtc,
    decimal WeightKg,
    string DateText);

public sealed class WeightGraphicViewModel : ObservableObject
{
    private const double ChartPointSpacing = 40d;

    private readonly GetMeasurementHistory _getHistory;
    private readonly ProfileId _profileId;
    private readonly LanguageService _languageService;
    private readonly ObservableCollection<WeightGraphicPoint> _points = [];
    private bool _isLoading;
    private string? _errorMessage;
    private WeightGraphicPoint? _selectedPoint;

    public WeightGraphicViewModel(
        GetMeasurementHistory getHistory,
        ProfileId profileId,
        LanguageService languageService)
    {
        _getHistory = getHistory;
        _profileId = profileId;
        _languageService = languageService;
        Points = new ReadOnlyObservableCollection<WeightGraphicPoint>(_points);
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public ReadOnlyObservableCollection<WeightGraphicPoint> Points { get; }

    public string WeightAxisLabel => _languageService.Get("Weight");

    public string DateAxisLabel => _languageService.Get("Date");

    public double ChartWidth => Math.Max(360d, Points.Count * ChartPointSpacing + 72d);

    public bool IsLegendVisible => _selectedPoint is not null;

    public string LegendText => _selectedPoint is null
        ? string.Empty
        : string.Format(
            CultureInfo.CurrentCulture,
            "{0}: {1}{2}{3}: {4} {5}",
            DateAxisLabel,
            _selectedPoint.MeasuredAtUtc.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture),
            Environment.NewLine,
            WeightAxisLabel,
            _selectedPoint.WeightKg.ToString("0.##", CultureInfo.CurrentCulture),
            _languageService.Get("Kg"));

    public decimal ChartMinimumWeight => Points.Count == 0 ? 0m : Points.Min(point => point.WeightKg) - 10m;

    public decimal ChartMaximumWeight => Points.Count == 0 ? 0m : Points.Max(point => point.WeightKg) + 10m;

    public bool HasPoints => Points.Count > 0;

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
    }

    public bool IsEmpty => !IsLoading && Points.Count == 0 && ErrorMessage is null;

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
    }

    public IAsyncRelayCommand LoadCommand { get; }

    public void SelectPoint(WeightGraphicPoint? point)
    {
        if (EqualityComparer<WeightGraphicPoint?>.Default.Equals(_selectedPoint, point))
        {
            return;
        }

        _selectedPoint = point;
        OnPropertyChanged(nameof(IsLegendVisible));
        OnPropertyChanged(nameof(LegendText));
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        SelectPoint(null);
        _points.Clear();

        try
        {
            var result = await _getHistory.ExecuteAsync(_profileId, CancellationToken.None);
            if (result.IsSuccess)
            {
                foreach (var measurement in result.Value.OrderBy(measurement => measurement.MeasuredAtUtc))
                {
                    _points.Add(new WeightGraphicPoint(
                        measurement.MeasuredAtUtc,
                        measurement.WeightKg,
                        measurement.MeasuredAtUtc.ToString("dd/MM", CultureInfo.CurrentCulture)));
                }
            }
            else
            {
                ErrorMessage = _languageService.Get("WeightGraphicLoadError");
            }
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasPoints));
            OnPropertyChanged(nameof(ChartWidth));
            OnPropertyChanged(nameof(ChartMinimumWeight));
            OnPropertyChanged(nameof(ChartMaximumWeight));
            OnPropertyChanged(nameof(IsEmpty));
        }
    }
}
