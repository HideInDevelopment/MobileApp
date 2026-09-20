using System.Collections.ObjectModel;
using Anthropometry.Application.Common;
using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Entitlements;
using Anthropometry.Application.Profiles;
using Anthropometry.App.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anthropometry.App.Features.Profiles;

public sealed class ProfileListViewModel : ObservableObject
{
    private readonly GetProfiles _getProfiles;
    private readonly DeleteProfile _deleteProfile;
    private readonly IProfileNavigation _navigation;
    private readonly LanguageService _languageService;
    private readonly IEntitlementProvider _entitlementProvider;
    private readonly ObservableCollection<ProfileDto> _profiles = [];
    private EntitlementSnapshot _entitlement = new(
        EntitlementTier.Free,
        SubscriptionState.Active,
        null,
        null,
        null);
    private bool _isLoading;
    private string? _errorMessage;

    public ProfileListViewModel(
        GetProfiles getProfiles,
        DeleteProfile deleteProfile,
        IProfileNavigation navigation,
        LanguageService languageService,
        IEntitlementProvider? entitlementProvider = null)
    {
        _getProfiles = getProfiles;
        _deleteProfile = deleteProfile;
        _navigation = navigation;
        _languageService = languageService;
        _entitlementProvider = entitlementProvider ?? FreeEntitlementProvider.Instance;
        Profiles = new ReadOnlyObservableCollection<ProfileDto>(_profiles);
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        CreateCommand = new AsyncRelayCommand(CreateAsync);
        SettingsCommand = new AsyncRelayCommand(_navigation.ShowSettingsAsync);
        HelpCommand = new AsyncRelayCommand(_navigation.ShowHelpAsync);
        SelectCommand = new AsyncRelayCommand<ProfileDto?>(SelectAsync);
        DeleteCommand = new AsyncRelayCommand<ProfileDto?>(DeleteAsync);
        ImportCommand = new AsyncRelayCommand(ImportAsync);
    }

    public ReadOnlyObservableCollection<ProfileDto> Profiles { get; }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public bool HasProfiles => _profiles.Count > 0;

    public bool CanAddProfile => _profiles.Count < FeatureAccessPolicy.GetMaximumProfiles(_entitlement);

    public bool CanImportProfile => _profiles.Count < FeatureAccessPolicy.GetMaximumProfiles(_entitlement)
        && FeatureAccessPolicy.CanUse(_entitlement, PremiumFeature.EncryptedProfileTransfer);

    public bool IsAddProfileLocked => _profiles.Count >= FeatureAccessPolicy.GetMaximumProfiles(_entitlement);

    public bool IsImportProfileLocked => !CanImportProfile;

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

    public IAsyncRelayCommand SettingsCommand { get; }

    public IAsyncRelayCommand HelpCommand { get; }

    public IAsyncRelayCommand<ProfileDto?> SelectCommand { get; }

    public IAsyncRelayCommand<ProfileDto?> DeleteCommand { get; }

    public IAsyncRelayCommand ImportCommand { get; }

    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        OnPropertyChanged(nameof(IsEmpty));
        try
        {
            _entitlement = await _entitlementProvider.GetCurrentAsync(CancellationToken.None);
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
                ErrorMessage = _languageService.Get("LoadProfilesError");
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

    private async Task CreateAsync()
    {
        if (IsAddProfileLocked)
        {
            await _navigation.ShowPremiumAsync();
            return;
        }

        await _navigation.CreateProfileAsync();
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
            ErrorMessage = _languageService.Get("DeleteProfileError");
            return;
        }

        await LoadAsync();
    }

    private async Task ImportAsync()
    {
        if (IsImportProfileLocked)
        {
            await _navigation.ShowPremiumAsync();
            return;
        }

        await _navigation.ImportProfileAsync();
        await LoadAsync();
    }

    private void NotifyProfileStateChanged()
    {
        OnPropertyChanged(nameof(HasProfiles));
        OnPropertyChanged(nameof(CanAddProfile));
        OnPropertyChanged(nameof(CanImportProfile));
        OnPropertyChanged(nameof(IsAddProfileLocked));
        OnPropertyChanged(nameof(IsImportProfileLocked));
        OnPropertyChanged(nameof(IsEmpty));
    }
}
