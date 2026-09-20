using Anthropometry.App.Localization;

namespace Anthropometry.App.Features.Profiles;

public partial class PassphrasePromptPage : ContentPage
{
    private readonly PassphrasePromptSession _session = new();

    public PassphrasePromptPage(bool requiresConfirmation, LanguageService languageService)
    {
        InitializeComponent();
        BindingContext = new PassphrasePromptViewModel(requiresConfirmation, languageService);
    }

    public Task<string?> Completion => _session.Completion;

    private async void OnSubmitClicked(object? sender, EventArgs e)
    {
        var viewModel = (PassphrasePromptViewModel)BindingContext;
        if (!viewModel.TrySubmit(out var passphrase))
        {
            return;
        }

        await _session.CloseAsync(passphrase, async () => await Navigation.PopModalAsync());
        viewModel.Cancel();
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        var viewModel = (PassphrasePromptViewModel)BindingContext;
        viewModel.Cancel();
        await _session.CloseAsync(null, async () => await Navigation.PopModalAsync());
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _session.Dismissed();
    }
}
