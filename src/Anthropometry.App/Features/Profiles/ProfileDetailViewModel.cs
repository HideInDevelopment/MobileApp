using Anthropometry.Application.Common;
using Anthropometry.Domain.Measurements;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anthropometry.App.Features.Profiles;

public sealed class ProfileDetailViewModel : ObservableObject
{
    private readonly IProfileNavigation _navigation;

    public ProfileDetailViewModel(ProfileDto profile, IProfileNavigation navigation)
    {
        Profile = profile;
        _navigation = navigation;
        AddWeightCommand = new AsyncRelayCommand(() => _navigation.CreateMeasurementAsync(Profile, MeasurementType.WeightOnly));
        AddMeasurementsCommand = new AsyncRelayCommand(() => _navigation.CreateMeasurementAsync(Profile, MeasurementType.WeightAndSizes));
        HistoryCommand = new AsyncRelayCommand(() => _navigation.ShowHistoryAsync(Profile));
        EditCommand = new AsyncRelayCommand(() => _navigation.RenameProfileAsync(Profile));
    }

    public ProfileDto Profile { get; }

    public IAsyncRelayCommand AddWeightCommand { get; }

    public IAsyncRelayCommand AddMeasurementsCommand { get; }

    public IAsyncRelayCommand HistoryCommand { get; }

    public IAsyncRelayCommand EditCommand { get; }
}
