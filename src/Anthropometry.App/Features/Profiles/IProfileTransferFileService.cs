using Anthropometry.Application.Profiles;

namespace Anthropometry.App.Features.Profiles;

public interface IProfileTransferFileService
{
    Task ShareAsync(ProfileExportFile file, string title, CancellationToken cancellationToken);

    Task<Stream?> PickCsvAsync(string title, CancellationToken cancellationToken);
}
