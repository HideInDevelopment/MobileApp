using System.Collections.ObjectModel;
using Anthropometry.Application.Common;
using Anthropometry.Application.Measurements;
using Anthropometry.Domain.Profiles;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anthropometry.App.Features.Measurements;

public sealed class MeasurementHistoryViewModel : ObservableObject
{
    private readonly GetMeasurementHistory _getHistory;
    private readonly ProfileId _profileId;
    private readonly IMeasurementNavigation _navigation;
    private readonly ObservableCollection<MeasurementDto> _measurements = [];
    private bool _isLoading;
    private string? _errorMessage;

    public MeasurementHistoryViewModel(GetMeasurementHistory getHistory, ProfileId profileId, IMeasurementNavigation navigation)
    {
        _getHistory = getHistory;
        _profileId = profileId;
        _navigation = navigation;
        Measurements = new ReadOnlyObservableCollection<MeasurementDto>(_measurements);
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SelectCommand = new AsyncRelayCommand<MeasurementDto?>(SelectAsync);
    }

    public ReadOnlyObservableCollection<MeasurementDto> Measurements { get; }

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

    public IAsyncRelayCommand<MeasurementDto?> SelectCommand { get; }

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
                    _measurements.Add(measurement);
                }
            }
            else
            {
                ErrorMessage = "We couldn't load history. Try again.";
            }
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    private Task SelectAsync(MeasurementDto? measurement)
        => measurement is null ? Task.CompletedTask : _navigation.ShowResultsAsync(measurement);
}
