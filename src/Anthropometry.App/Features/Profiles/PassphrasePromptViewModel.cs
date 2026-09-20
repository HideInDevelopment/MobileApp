using Anthropometry.App.Localization;
using Anthropometry.Application.Profiles;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Anthropometry.App.Features.Profiles;

public sealed class PassphrasePromptViewModel : ObservableObject
{
    private readonly LanguageService _languageService;
    private string _passphrase = string.Empty;
    private string _confirmation = string.Empty;
    private string? _validationMessage;
    private bool _isCancelled;

    public PassphrasePromptViewModel(bool requiresConfirmation, LanguageService languageService)
    {
        RequiresConfirmation = requiresConfirmation;
        _languageService = languageService;
    }

    public bool RequiresConfirmation { get; }

    public string Passphrase
    {
        get => _passphrase;
        set => SetProperty(ref _passphrase, value);
    }

    public string Confirmation
    {
        get => _confirmation;
        set => SetProperty(ref _confirmation, value);
    }

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public bool IsCancelled
    {
        get => _isCancelled;
        private set => SetProperty(ref _isCancelled, value);
    }

    public bool TrySubmit(out string? submitted)
    {
        submitted = null;
        IsCancelled = false;
        if (!ProfileTransferProtection.ValidateTransferCredential(Passphrase).IsSuccess)
        {
            ValidationMessage = _languageService.Get("ProfileTransferCodeInvalid");
            return false;
        }

        if (RequiresConfirmation && !string.Equals(Passphrase, Confirmation, StringComparison.Ordinal))
        {
            ValidationMessage = _languageService.Get("ProfileTransferPassphraseMismatch");
            return false;
        }

        ValidationMessage = null;
        submitted = Passphrase;
        return true;
    }

    public void Cancel()
    {
        Passphrase = string.Empty;
        Confirmation = string.Empty;
        ValidationMessage = null;
        IsCancelled = true;
    }
}
