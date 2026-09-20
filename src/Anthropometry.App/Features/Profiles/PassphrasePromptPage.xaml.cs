using Anthropometry.App.Localization;

namespace Anthropometry.App.Features.Profiles;

public partial class PassphrasePromptPage : ContentPage
{
    private readonly TaskCompletionSource<string?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public PassphrasePromptPage(bool requiresConfirmation, LanguageService languageService)
    {
        InitializeComponent();
        BindingContext = new PassphrasePromptViewModel(requiresConfirmation, languageService);
    }

    public Task<string?> Completion => _completion.Task;

    private async void OnSubmitClicked(object? sender, EventArgs e)
    {
        var viewModel = (PassphrasePromptViewModel)BindingContext;
        if (!viewModel.TrySubmit(out var passphrase))
        {
            return;
        }

        _completion.TrySetResult(passphrase);
        viewModel.Cancel();
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        var viewModel = (PassphrasePromptViewModel)BindingContext;
        viewModel.Cancel();
        _completion.TrySetResult(null);
        await Navigation.PopModalAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _completion.TrySetResult(null);
    }
}
