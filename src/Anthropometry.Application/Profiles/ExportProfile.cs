using System.Globalization;
using System.Text;
using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Common;
using Anthropometry.Domain.Common;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Profiles;

public sealed record ExportProfileCommand(ProfileId ProfileId, string Passphrase);

public sealed class ExportProfile
{
    private readonly IProfileTransferRepository _repository;
    private readonly IClock _clock;

    public ExportProfile(IProfileTransferRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<Result<ProfileExportFile>> ExecuteAsync(
        ExportProfileCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await _repository.GetSnapshotAsync(command.ProfileId, cancellationToken);
            if (snapshot is null)
            {
                return Result.Failure<ProfileExportFile>(ApplicationErrors.ProfileNotFound);
            }

            var protectedContent = ProfileTransferProtection.Protect(
                ProfileTransferCsvSerializer.Serialize(snapshot),
                command.Passphrase);
            if (!protectedContent.IsSuccess)
            {
                return Result.Failure<ProfileExportFile>(protectedContent.Error!);
            }

            var safeName = SanitizeName(snapshot.Profile.Name);
            var fileName = $"anthropometry-{safeName}-{_clock.UtcNow:yyyyMMdd}.anthropometry";
            return Result.Success(new ProfileExportFile(
                fileName,
                protectedContent.Value));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Result.Failure<ProfileExportFile>(ApplicationErrors.PersistenceUnavailable);
        }
    }

    private static string SanitizeName(string name)
    {
        var builder = new StringBuilder();
        foreach (var character in name.Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        var sanitized = builder.ToString().Trim('-');
        return string.IsNullOrEmpty(sanitized) ? "profile" : sanitized;
    }
}
