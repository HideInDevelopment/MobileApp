using Anthropometry.Application.Common;
using Anthropometry.Application.Measurements;
using Anthropometry.App.Localization;
using Anthropometry.Domain.Measurements;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anthropometry.App.Features.Profiles;

public sealed class ProfileDetailViewModel : ObservableObject
{
    private readonly IProfileNavigation _navigation;
    private readonly GetMeasurementHistory _getHistory;
    private readonly GenerateSampleMeasurementHistory _generateSampleData;
    private readonly LanguageService _languageService;
    private bool _isLoading;
    private string? _errorMessage;
    private string? _statusMessage;
    private bool _canAddWeight;
    private bool _canGenerateSampleData;
    private int _measurementCount;

    public ProfileDetailViewModel(
        ProfileDto profile,
        GetMeasurementHistory getHistory,
        GenerateSampleMeasurementHistory generateSampleData,
        IProfileNavigation navigation,
        LanguageService languageService)
    {
        Profile = profile;
        _getHistory = getHistory;
        _generateSampleData = generateSampleData;
        _navigation = navigation;
        _languageService = languageService;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        AddWeightCommand = new AsyncRelayCommand(() => _navigation.CreateMeasurementAsync(Profile, MeasurementType.WeightOnly), () => CanAddWeight);
        AddMeasurementsCommand = new AsyncRelayCommand(() => _navigation.CreateMeasurementAsync(Profile, MeasurementType.WeightAndSizes));
        HistoryCommand = new AsyncRelayCommand(() => _navigation.ShowHistoryAsync(Profile));
        EditCommand = new AsyncRelayCommand(() => _navigation.RenameProfileAsync(Profile));
        GenerateSampleDataCommand = new AsyncRelayCommand(GenerateSampleDataAsync, () => CanGenerateSampleData);
    }

    public ProfileDto Profile { get; }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool CanAddWeight
    {
        get => _canAddWeight;
        private set
        {
            if (SetProperty(ref _canAddWeight, value))
            {
                AddWeightCommand.NotifyCanExecuteChanged();
                RefreshSampleDataAvailability();
            }
        }
    }

    public bool CanGenerateSampleData
    {
        get => _canGenerateSampleData;
        private set
        {
            if (SetProperty(ref _canGenerateSampleData, value))
            {
                GenerateSampleDataCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand AddWeightCommand { get; }

    public IAsyncRelayCommand AddMeasurementsCommand { get; }

    public IAsyncRelayCommand HistoryCommand { get; }

    public IAsyncRelayCommand EditCommand { get; }

    public IAsyncRelayCommand GenerateSampleDataCommand { get; }

    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = null;
        _measurementCount = 0;
        RefreshSampleDataAvailability();
        CanAddWeight = false;
        try
        {
            var result = await _getHistory.ExecuteAsync(Profile.Id, CancellationToken.None);
            if (result.IsSuccess)
            {
                _measurementCount = result.Value.Count;
                CanAddWeight = result.Value.Any(measurement => measurement.Type == MeasurementType.WeightAndSizes);
                RefreshSampleDataAvailability();
            }
            else
            {
                ErrorMessage = _languageService.Get("ProfileDetailsError");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void RefreshSampleDataAvailability()
    {
        CanGenerateSampleData = CanAddWeight && _measurementCount < 30;
    }

    private async Task GenerateSampleDataAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = null;
        try
        {
            var result = await _generateSampleData.ExecuteAsync(
                new GenerateSampleMeasurementHistoryCommand(Profile.Id),
                CancellationToken.None);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error?.Code == "sampleData.sizeMeasurement.required"
                    ? _languageService.Get("SampleDataRequiresMeasurement")
                    : _languageService.Get("SampleDataError");
                return;
            }

            await LoadAsync();
            StatusMessage = _languageService.Get("SampleDataGenerated");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
