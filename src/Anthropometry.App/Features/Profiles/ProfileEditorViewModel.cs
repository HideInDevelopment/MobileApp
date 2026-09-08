using Anthropometry.Application.Common;
using Anthropometry.Application.Profiles;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anthropometry.App.Features.Profiles;

public sealed class ProfileEditorViewModel : ObservableObject
{
    private readonly CreateProfile _createProfile;
    private readonly RenameProfile _renameProfile;
    private readonly ProfileDto? _existingProfile;
    private readonly IProfileNavigation _navigation;
    private string _name;
    private string? _validationMessage;
    private string? _errorMessage;
    private bool _isBusy;
    private bool _isCompleted;

    public ProfileEditorViewModel(
        CreateProfile createProfile,
        RenameProfile renameProfile,
        ProfileDto? existingProfile,
        IProfileNavigation navigation)
    {
        _createProfile = createProfile;
        _renameProfile = renameProfile;
        _existingProfile = existingProfile;
        _navigation = navigation;
        _name = existingProfile?.Name ?? string.Empty;
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new AsyncRelayCommand(_navigation.CancelAsync);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Title => _existingProfile is null ? "New profile" : "Rename profile";

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
            ValidationMessage = "A profile name is required.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = _existingProfile is null
                ? await _createProfile.ExecuteAsync(Name, CancellationToken.None)
                : await _renameProfile.ExecuteAsync(_existingProfile.Id, Name, CancellationToken.None);
            if (!result.IsSuccess)
            {
                ErrorMessage = "We couldn't save this profile. Try again.";
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
}
