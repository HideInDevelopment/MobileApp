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
    private readonly GetMeasurementHistory _getHistory;
    private readonly ProfileId _profileId;
    private readonly LanguageService _languageService;
    private readonly ObservableCollection<WeightGraphicPoint> _points = [];
    private bool _isLoading;
    private string? _errorMessage;

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

    public double ChartWidth => Math.Max(360d, Points.Count * 64d + 72d);

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

    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
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
            OnPropertyChanged(nameof(IsEmpty));
        }
    }
}
