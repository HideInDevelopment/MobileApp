using Anthropometry.Application.Common;
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
        NewMeasurementCommand = new AsyncRelayCommand(() => _navigation.CreateMeasurementAsync(Profile));
        HistoryCommand = new AsyncRelayCommand(() => _navigation.ShowHistoryAsync(Profile));
        RenameCommand = new AsyncRelayCommand(() => _navigation.RenameProfileAsync(Profile));
    }

    public ProfileDto Profile { get; }

    public IAsyncRelayCommand NewMeasurementCommand { get; }

    public IAsyncRelayCommand HistoryCommand { get; }

    public IAsyncRelayCommand RenameCommand { get; }
}
