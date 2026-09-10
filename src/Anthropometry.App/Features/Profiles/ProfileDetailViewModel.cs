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
    private readonly LanguageService _languageService;
    private bool _isLoading;
    private string? _errorMessage;
    private bool _canAddWeight;

    public ProfileDetailViewModel(
        ProfileDto profile,
        GetMeasurementHistory getHistory,
        IProfileNavigation navigation,
        LanguageService languageService)
    {
        Profile = profile;
        _getHistory = getHistory;
        _navigation = navigation;
        _languageService = languageService;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        AddWeightCommand = new AsyncRelayCommand(() => _navigation.CreateMeasurementAsync(Profile, MeasurementType.WeightOnly), () => CanAddWeight);
        AddMeasurementsCommand = new AsyncRelayCommand(() => _navigation.CreateMeasurementAsync(Profile, MeasurementType.WeightAndSizes));
        HistoryCommand = new AsyncRelayCommand(() => _navigation.ShowHistoryAsync(Profile));
        EditCommand = new AsyncRelayCommand(() => _navigation.RenameProfileAsync(Profile));
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

    public bool CanAddWeight
    {
        get => _canAddWeight;
        private set
        {
            if (SetProperty(ref _canAddWeight, value))
            {
                AddWeightCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand AddWeightCommand { get; }

    public IAsyncRelayCommand AddMeasurementsCommand { get; }

    public IAsyncRelayCommand HistoryCommand { get; }

    public IAsyncRelayCommand EditCommand { get; }

    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        CanAddWeight = false;
        try
        {
            var result = await _getHistory.ExecuteAsync(Profile.Id, CancellationToken.None);
            if (result.IsSuccess)
            {
                CanAddWeight = result.Value.Any(measurement => measurement.Type == MeasurementType.WeightAndSizes);
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
}
