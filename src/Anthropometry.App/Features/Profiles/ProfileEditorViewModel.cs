using System.Globalization;
using Anthropometry.Application.Common;
using Anthropometry.Application.Profiles;
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
    private string _name;
    private string _heightText;
    private string _ageText;
    private ActivityLevelOption? _selectedActivityLevel;
    private GenderOption? _selectedGender;
    private string? _validationMessage;
    private string? _errorMessage;
    private bool _isBusy;
    private bool _isCompleted;

    public ProfileEditorViewModel(
        CreateProfile createProfile,
        UpdateProfile updateProfile,
        ProfileDto? existingProfile,
        IProfileNavigation navigation,
        LanguageService languageService)
    {
        _createProfile = createProfile;
        _updateProfile = updateProfile;
        _existingProfile = existingProfile;
        _navigation = navigation;
        _languageService = languageService;
        _name = existingProfile?.Name ?? string.Empty;
        _heightText = existingProfile?.Settings?.HeightCm.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
        _ageText = existingProfile?.Settings?.AgeYears.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
        ActivityLevels =
        [
            new(ActivityLevel.Sedentary, _languageService.Get("Sedentary")),
            new(ActivityLevel.Light, _languageService.Get("LightlyActive")),
            new(ActivityLevel.Moderate, _languageService.Get("ModeratelyActive")),
            new(ActivityLevel.High, _languageService.Get("HighlyActive")),
            new(ActivityLevel.VeryHigh, _languageService.Get("VeryHighlyActive"))
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
        if ((!decimal.TryParse(HeightText, NumberStyles.Number, CultureInfo.CurrentCulture, out var height)
                && !decimal.TryParse(HeightText, NumberStyles.Number, CultureInfo.InvariantCulture, out height))
            || !int.TryParse(AgeText, NumberStyles.Integer, CultureInfo.CurrentCulture, out var age)
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
}
