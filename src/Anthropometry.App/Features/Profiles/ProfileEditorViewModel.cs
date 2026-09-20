using System.Globalization;
using Anthropometry.Application.Common;
using Anthropometry.Application.Entitlements;
using Anthropometry.Application.Profiles;
using Anthropometry.App.Display;
using Anthropometry.App.Features.Help;
using Anthropometry.App.Localization;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Profiles;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anthropometry.App.Features.Profiles;

public sealed class ProfileEditorViewModel : ObservableObject
{
    private readonly CreateProfile _createProfile;
    private readonly UpdateProfile _updateProfile;
    private readonly ProfileDto? _existingProfile;
    private readonly IProfileNavigation _navigation;
    private readonly LanguageService _languageService;
    private readonly DisplayPreferencesService _displayPreferences;
    private readonly EntitledDisplayPreferences _entitledDisplayPreferences;
    private string _name;
    private string _heightText;
    private string _ageText;
    private ActivityLevelOption? _selectedActivityLevel;
    private GenderOption? _selectedGender;
    private string? _validationMessage;
    private string? _errorMessage;
    private bool _isBusy;
    private bool _isCompleted;
    private string _heightUnitCode;

    public ProfileEditorViewModel(
        CreateProfile createProfile,
        UpdateProfile updateProfile,
        ProfileDto? existingProfile,
        IProfileNavigation navigation,
        LanguageService languageService,
        DisplayPreferencesService displayPreferences,
        EntitlementSnapshot? entitlement = null)
    {
        _createProfile = createProfile;
        _updateProfile = updateProfile;
        _existingProfile = existingProfile;
        _navigation = navigation;
        _languageService = languageService;
        _displayPreferences = displayPreferences;
        _entitledDisplayPreferences = new EntitledDisplayPreferences(
            _displayPreferences,
            entitlement ?? new EntitlementSnapshot(EntitlementTier.Free, SubscriptionState.Active, null, null, null));
        _heightUnitCode = _entitledDisplayPreferences.HeightUnitCode;
        _name = existingProfile?.Name ?? string.Empty;
        _heightText = existingProfile?.Settings is { } existingSettings
            ? FormatHeight(existingSettings.HeightCm, _entitledDisplayPreferences.HeightUnitCode)
            : string.Empty;
        _ageText = existingProfile?.Settings?.AgeYears.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
        ActivityLevels =
        [
            ActivityLevelOption.Create(ActivityLevel.Sedentary, _languageService),
            ActivityLevelOption.Create(ActivityLevel.Light, _languageService),
            ActivityLevelOption.Create(ActivityLevel.Moderate, _languageService),
            ActivityLevelOption.Create(ActivityLevel.High, _languageService),
            ActivityLevelOption.Create(ActivityLevel.VeryHigh, _languageService)
        ];
        GenderOptions =
        [
            new(ProfileGender.Male, _languageService.Get("Male")),
            new(ProfileGender.Female, _languageService.Get("Female"))
        ];
        _selectedActivityLevel = existingProfile?.Settings is { } settings
            ? ActivityLevels.SingleOrDefault(option => option.Value == settings.ActivityLevel)
            : null;
        _selectedGender = GenderOptions.Single(option => option.Value == (existingProfile?.Gender ?? ProfileGender.Male));
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new AsyncRelayCommand(_navigation.CancelAsync);
        ShowGuidanceCommand = new AsyncRelayCommand<GuidanceTopic>(_navigation.ShowGuidanceAsync);
        SelectGenderCommand = new RelayCommand<string?>(SelectGender);
        SelectActivityLevelCommand = new RelayCommand<string?>(SelectActivityLevel);
        _displayPreferences.PreferencesChanged += OnDisplayPreferencesChanged;
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string HeightText
    {
        get => _heightText;
        set => SetProperty(ref _heightText, value);
    }

    public string HeightUnitText => _languageService.Get(
        _entitledDisplayPreferences.HeightUnitCode == DisplayPreferencesService.FeetCode ? "Ft" : "M");

    public string AgeText
    {
        get => _ageText;
        set => SetProperty(ref _ageText, value);
    }

    public IReadOnlyList<ActivityLevelOption> ActivityLevels { get; }

    public ActivityLevelOption? SelectedActivityLevel
    {
        get => _selectedActivityLevel;
        set => SetProperty(ref _selectedActivityLevel, value);
    }

    public IReadOnlyList<GenderOption> GenderOptions { get; }

    public GenderOption? SelectedGender
    {
        get => _selectedGender;
        set => SetProperty(ref _selectedGender, value);
    }

    public string Title => _existingProfile is null
        ? _languageService.Get("CreateProfileTitle")
        : _languageService.Get("EditProfileTitle");

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        private set => SetProperty(ref _isCompleted, value);
    }

    public IAsyncRelayCommand SaveCommand { get; }

    public IAsyncRelayCommand CancelCommand { get; }

    public IAsyncRelayCommand<GuidanceTopic> ShowGuidanceCommand { get; }

    public IRelayCommand<string?> SelectGenderCommand { get; }

    public IRelayCommand<string?> SelectActivityLevelCommand { get; }

    private async Task SaveAsync()
    {
        ValidationMessage = null;
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(Name))
        {
            ValidationMessage = _languageService.Get("ProfileNameRequired");
            return;
        }

        if (!TryCreateSettings(out var settings))
        {
            ValidationMessage = _languageService.Get("ValidProfileSettings");
            return;
        }

        if (SelectedGender is null)
        {
            ValidationMessage = _languageService.Get("GenderRequired");
            return;
        }

        IsBusy = true;
        try
        {
            var result = _existingProfile is null
                ? await _createProfile.ExecuteAsync(new CreateProfileCommand(Name, settings, SelectedGender.Value), CancellationToken.None)
                : await _updateProfile.ExecuteAsync(_existingProfile.Id, Name, settings, CancellationToken.None, SelectedGender.Value);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error!.Code == "profile.limit.reached"
                    ? _languageService.Get("ProfileLimitReached")
                    : _languageService.Get("SaveProfileError");
                return;
            }

            IsCompleted = true;
            await _navigation.CloseEditorAsync(result.Value);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool TryCreateSettings(out ProfileSettingsInput settings)
    {
        if (!TryParseDecimal(HeightText, out var enteredHeight)
            || !int.TryParse(AgeText, NumberStyles.Integer, CultureInfo.CurrentCulture, out var age)
            || !TryConvertHeight(enteredHeight, _entitledDisplayPreferences.HeightUnitCode, out var height)
            || height is < 50m or > 300m
            || age is < 1 or > 120
            || SelectedActivityLevel is null)
        {
            settings = null!;
            return false;
        }

        settings = new ProfileSettingsInput(height, age, SelectedActivityLevel.Value);
        return true;
    }

    private static bool TryConvertHeight(decimal enteredHeight, string unitCode, out decimal height)
    {
        height = DisplayPreferencesService.ConvertHeightToMetric(enteredHeight, unitCode);
        return height > 0;
    }

    private void SelectGender(string? value)
    {
        if (Enum.TryParse<ProfileGender>(value, ignoreCase: true, out var gender))
        {
            SelectedGender = GenderOptions.SingleOrDefault(option => option.Value == gender);
        }
    }

    private void SelectActivityLevel(string? value)
    {
        if (Enum.TryParse<ActivityLevel>(value, ignoreCase: true, out var activityLevel))
        {
            SelectedActivityLevel = ActivityLevels.SingleOrDefault(option => option.Value == activityLevel);
        }
    }

    private static string FormatHeight(decimal heightCm, string unitCode)
    {
        var displayHeight = DisplayPreferencesService.ConvertHeightToDisplay(heightCm, unitCode);
        return displayHeight.ToString("0.##", CultureInfo.CurrentCulture);
    }

    private static bool TryParseDecimal(string value, out decimal result)
    {
        var normalized = value.Trim().Replace(',', '.');
        return decimal.TryParse(
            normalized,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out result);
    }

    private void OnDisplayPreferencesChanged(object? sender, EventArgs e)
    {
        if (TryParseDecimal(_heightText, out var enteredHeight)
            && TryConvertHeight(enteredHeight, _heightUnitCode, out var heightCm))
        {
            _heightText = FormatHeight(heightCm, _entitledDisplayPreferences.HeightUnitCode);
            OnPropertyChanged(nameof(HeightText));
        }

        _heightUnitCode = _entitledDisplayPreferences.HeightUnitCode;
        OnPropertyChanged(nameof(HeightUnitText));
    }
}
