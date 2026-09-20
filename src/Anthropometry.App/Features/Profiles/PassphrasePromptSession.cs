namespace Anthropometry.App.Features.Profiles;

public sealed class PassphrasePromptSession
{
    private readonly TaskCompletionSource<string?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<string?> Completion => _completion.Task;

    public bool IsClosing { get; private set; }

    public async Task CloseAsync(string? result, Func<Task> closeModalAsync)
    {
        if (IsClosing)
        {
            return;
        }

        IsClosing = true;
        try
        {
            await closeModalAsync();
            _completion.TrySetResult(result);
        }
        catch (Exception)
        {
            _completion.TrySetResult(null);
        }
    }

    public void Dismissed()
    {
        if (!IsClosing)
        {
            _completion.TrySetResult(null);
        }
    }
}
