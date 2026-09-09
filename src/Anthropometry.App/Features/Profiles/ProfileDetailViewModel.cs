using Anthropometry.Application.Common;
using Anthropometry.Application.Measurements;
using Anthropometry.Domain.Measurements;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anthropometry.App.Features.Profiles;

public sealed class ProfileDetailViewModel : ObservableObject
{
    private readonly IProfileNavigation _navigation;
    private readonly GetMeasurementHistory _getHistory;
    private bool _isLoading;
    private string? _errorMessage;
    private bool _showWarningIcon;

    public ProfileDetailViewModel(ProfileDto profile, GetMeasurementHistory getHistory, IProfileNavigation navigation)
    {
        Profile = profile;
        _getHistory = getHistory;
        _navigation = navigation;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        AddWeightCommand = new AsyncRelayCommand(() => _navigation.CreateMeasurementAsync(Profile, MeasurementType.WeightOnly));
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

    public bool ShowWarningIcon
    {
        get => _showWarningIcon;
        private set => SetProperty(ref _showWarningIcon, value);
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
        ShowWarningIcon = false;
        try
        {
            var result = await _getHistory.ExecuteAsync(Profile.Id, CancellationToken.None);
            if (result.IsSuccess)
            {
                ShowWarningIcon = result.Value.Count > 0 && result.Value[0].Type == MeasurementType.WeightOnly;
            }
            else
            {
                ErrorMessage = "We couldn't load profile details. Try again.";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
