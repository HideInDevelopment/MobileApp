using Anthropometry.App.Features.Profiles;
using Anthropometry.App.Tests.Support;

namespace Anthropometry.App.Tests.Features.Profiles;

public sealed class PassphrasePromptViewModelTests
{
    private const string Passphrase = "correct horse battery staple";
    private const string TransferCode = "4827193066428501";

    [Fact]
    public void Valid_transfer_code_can_be_submitted_for_import()
    {
        var viewModel = new PassphrasePromptViewModel(false, TestData.LanguageService())
        {
            Passphrase = TransferCode
        };

        var accepted = viewModel.TrySubmit(out var submitted);

        Assert.True(accepted);
        Assert.Equal(TransferCode, submitted);
        Assert.Null(viewModel.ValidationMessage);
    }

    [Fact]
    public void Legacy_passphrase_can_still_be_submitted_for_import()
    {
        var viewModel = new PassphrasePromptViewModel(false, TestData.LanguageService())
        {
            Passphrase = Passphrase
        };

        var accepted = viewModel.TrySubmit(out var submitted);

        Assert.True(accepted);
        Assert.Equal(Passphrase, submitted);
    }

    [Fact]
    public void Invalid_transfer_credential_is_rejected()
    {
        var viewModel = new PassphrasePromptViewModel(false, TestData.LanguageService())
        {
            Passphrase = "1234"
        };

        var accepted = viewModel.TrySubmit(out var submitted);

        Assert.False(accepted);
        Assert.Null(submitted);
        Assert.NotNull(viewModel.ValidationMessage);
    }

    [Fact]
    public void Cancel_clears_the_prompt_state()
    {
        var viewModel = new PassphrasePromptViewModel(false, TestData.LanguageService())
        {
            Passphrase = Passphrase
        };

        viewModel.Cancel();

        Assert.True(viewModel.IsCancelled);
        Assert.Equal(string.Empty, viewModel.Passphrase);
        Assert.Equal(string.Empty, viewModel.Confirmation);
    }

    [Fact]
    public async Task Prompt_completion_waits_until_the_modal_is_closed()
    {
        var session = new PassphrasePromptSession();
        var modalClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var closeTask = session.CloseAsync(
            Passphrase,
            () => modalClosed.Task);

        Assert.False(session.Completion.IsCompleted);
        modalClosed.SetResult();
        await closeTask;

        Assert.Equal(Passphrase, await session.Completion);
    }

    [Fact]
    public async Task Prompt_completion_is_cancelled_when_modal_dismissal_fails()
    {
        var session = new PassphrasePromptSession();

        await session.CloseAsync(Passphrase, () => throw new InvalidOperationException());

        Assert.Null(await session.Completion);
    }

    [Fact]
    public async Task Unexpected_modal_dismissal_completes_with_no_passphrase()
    {
        var session = new PassphrasePromptSession();

        session.Dismissed();

        Assert.Null(await session.Completion);
    }
}
