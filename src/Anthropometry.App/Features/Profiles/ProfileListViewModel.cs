using System.Windows.Input;
using System.Collections.ObjectModel;
using Anthropometry.Application.Common;
using Anthropometry.Application.Profiles;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anthropometry.App.Features.Profiles;

public sealed class ProfileListViewModel : ObservableObject
{
    private readonly GetProfiles _getProfiles;
    private readonly DeleteProfile _deleteProfile;
    private readonly IProfileNavigation _navigation;
    private readonly ObservableCollection<ProfileDto> _profiles = [];
    private bool _isLoading;
    private string? _errorMessage;

    public ProfileListViewModel(GetProfiles getProfiles, DeleteProfile deleteProfile, IProfileNavigation navigation)
    {
        _getProfiles = getProfiles;
        _deleteProfile = deleteProfile;
        _navigation = navigation;
        Profiles = new ReadOnlyObservableCollection<ProfileDto>(_profiles);
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        CreateCommand = new AsyncRelayCommand(_navigation.CreateProfileAsync, () => CanAddProfile);
        SettingsCommand = new RelayCommand(() => { });
        HelpCommand = new RelayCommand(() => { });
        SelectCommand = new AsyncRelayCommand<ProfileDto?>(SelectAsync);
        DeleteCommand = new AsyncRelayCommand<ProfileDto?>(DeleteAsync);
    }

    public ReadOnlyObservableCollection<ProfileDto> Profiles { get; }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public bool HasProfiles => _profiles.Count > 0;

    public bool CanAddProfile => _profiles.Count < 4;

    public bool IsEmpty => !IsLoading && !HasProfiles && ErrorMessage is null;

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

    public IAsyncRelayCommand CreateCommand { get; }

    public ICommand SettingsCommand { get; }

    public ICommand HelpCommand { get; }

    public IAsyncRelayCommand<ProfileDto?> SelectCommand { get; }

    public IAsyncRelayCommand<ProfileDto?> DeleteCommand { get; }

    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        OnPropertyChanged(nameof(IsEmpty));
        try
        {
            var result = await _getProfiles.ExecuteAsync(CancellationToken.None);
            _profiles.Clear();
            if (result.IsSuccess)
            {
                foreach (var profile in result.Value)
                {
                    _profiles.Add(profile);
                }
            }
            else
            {
                ErrorMessage = "We couldn't load profiles. Try again.";
            }
            NotifyProfileStateChanged();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    private Task SelectAsync(ProfileDto? profile)
        => profile is null ? Task.CompletedTask : _navigation.SelectProfileAsync(profile);

    private async Task DeleteAsync(ProfileDto? profile)
    {
        if (profile is null || !await _navigation.ConfirmDeleteAsync(profile))
        {
            return;
        }

        var result = await _deleteProfile.ExecuteAsync(profile.Id, CancellationToken.None);
        if (!result.IsSuccess)
        {
            ErrorMessage = "We couldn't delete this profile. Try again.";
            return;
        }

        await LoadAsync();
    }

    private void NotifyProfileStateChanged()
    {
        OnPropertyChanged(nameof(HasProfiles));
        OnPropertyChanged(nameof(CanAddProfile));
        OnPropertyChanged(nameof(IsEmpty));
        CreateCommand.NotifyCanExecuteChanged();
    }
}
