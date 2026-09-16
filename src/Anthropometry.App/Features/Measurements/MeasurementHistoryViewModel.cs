using System.Collections.ObjectModel;
using System.Globalization;
using Anthropometry.Application.Common;
using Anthropometry.Application.Measurements;
using Anthropometry.App.Display;
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
    private readonly DisplayPreferencesService _displayPreferences;
    private readonly ObservableCollection<MeasurementHistoryItem> _measurements = [];
    private bool _isLoading;
    private string? _errorMessage;

    public MeasurementHistoryViewModel(
        GetMeasurementHistory getHistory,
        ProfileId profileId,
        IMeasurementNavigation navigation,
        LanguageService languageService,
        DisplayPreferencesService displayPreferences)
    {
        _getHistory = getHistory;
        _profileId = profileId;
        _navigation = navigation;
        _languageService = languageService;
        _displayPreferences = displayPreferences;
        Measurements = new ReadOnlyObservableCollection<MeasurementHistoryItem>(_measurements);
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SelectCommand = new AsyncRelayCommand<MeasurementHistoryItem?>(SelectAsync);
        ChartsCommand = new AsyncRelayCommand(() => _navigation.ShowChartOptionsAsync(_profileId));
        _displayPreferences.PreferencesChanged += OnDisplayPreferencesChanged;
        _languageService.LanguageChanged += OnLanguageChanged;
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

    public IAsyncRelayCommand ChartsCommand { get; }

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
                    _measurements.Add(CreateItem(measurement));
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

    private MeasurementHistoryItem CreateItem(MeasurementDto measurement)
        => new(
            measurement,
            _displayPreferences.FormatDate(measurement.MeasuredAtUtc),
            measurement.Type == MeasurementType.WeightOnly
                ? _languageService.Get("WeightOnly")
                : _languageService.Get("WeightAndSizes"),
            string.Format(
                CultureInfo.CurrentCulture,
                "{0}: {1:0.##} {2}",
                _languageService.Get("Weight"),
                _displayPreferences.ToDisplayWeight(measurement.WeightKg),
                _languageService.Get(_displayPreferences.WeightUnitCode == DisplayPreferencesService.PoundsCode ? "Lb" : "Kg")),
            string.Format(
                CultureInfo.CurrentCulture,
                "{0}: {1:0.##} {2}",
                _languageService.Get("Height"),
                _displayPreferences.ToDisplayHeight(measurement.HeightCm),
                _languageService.Get(_displayPreferences.HeightUnitCode == DisplayPreferencesService.FeetCode ? "Ft" : "M")));

    private void OnDisplayPreferencesChanged(object? sender, EventArgs e)
        => RefreshItems();

    private void OnLanguageChanged(object? sender, EventArgs e)
        => RefreshItems();

    private void RefreshItems()
    {
        if (_measurements.Count == 0)
        {
            return;
        }

        var measurements = _measurements.Select(item => item.Measurement).ToArray();
        _measurements.Clear();
        foreach (var measurement in measurements)
        {
            _measurements.Add(CreateItem(measurement));
        }
    }
}
