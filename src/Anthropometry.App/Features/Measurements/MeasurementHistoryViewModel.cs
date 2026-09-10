using System.Collections.ObjectModel;
using System.Globalization;
using Anthropometry.Application.Common;
using Anthropometry.Application.Measurements;
using Anthropometry.App.Localization;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anthropometry.App.Features.Measurements;

public sealed class MeasurementHistoryViewModel : ObservableObject
{
    private readonly GetMeasurementHistory _getHistory;
    private readonly ProfileId _profileId;
    private readonly IMeasurementNavigation _navigation;
    private readonly LanguageService _languageService;
    private readonly ObservableCollection<MeasurementHistoryItem> _measurements = [];
    private bool _isLoading;
    private string? _errorMessage;

    public MeasurementHistoryViewModel(
        GetMeasurementHistory getHistory,
        ProfileId profileId,
        IMeasurementNavigation navigation,
        LanguageService languageService)
    {
        _getHistory = getHistory;
        _profileId = profileId;
        _navigation = navigation;
        _languageService = languageService;
        Measurements = new ReadOnlyObservableCollection<MeasurementHistoryItem>(_measurements);
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SelectCommand = new AsyncRelayCommand<MeasurementHistoryItem?>(SelectAsync);
    }

    public ReadOnlyObservableCollection<MeasurementHistoryItem> Measurements { get; }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public bool IsEmpty => !IsLoading && Measurements.Count == 0 && ErrorMessage is null;

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

    public IAsyncRelayCommand<MeasurementHistoryItem?> SelectCommand { get; }

    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var result = await _getHistory.ExecuteAsync(_profileId, CancellationToken.None);
            _measurements.Clear();
            if (result.IsSuccess)
            {
                foreach (var measurement in result.Value)
                {
                    _measurements.Add(new MeasurementHistoryItem(
                        measurement,
                        measurement.MeasuredAtUtc.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture),
                        measurement.Type == MeasurementType.WeightOnly
                            ? _languageService.Get("WeightOnly")
                            : _languageService.Get("WeightAndSizes"),
                        string.Format(CultureInfo.CurrentCulture, "{0}: {1} {2}",
                            _languageService.Get("Weight"), measurement.WeightKg, _languageService.Get("Kg")),
                        string.Format(CultureInfo.CurrentCulture, "{0}: {1} {2}",
                            _languageService.Get("Height"), measurement.HeightCm, _languageService.Get("Cm"))));
                }
            }
            else
            {
                ErrorMessage = _languageService.Get("LoadHistoryError");
            }
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    private Task SelectAsync(MeasurementHistoryItem? item)
        => item is null || !item.CanViewResults
            ? Task.CompletedTask
            : _navigation.ShowResultsAsync(item.Measurement);
}
