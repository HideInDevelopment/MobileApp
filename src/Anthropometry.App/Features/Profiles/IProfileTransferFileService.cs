using Anthropometry.Application.Profiles;

namespace Anthropometry.App.Features.Profiles;

public interface IProfileTransferFileService
{
    Task ShareAsync(ProfileExportFile file, string title, CancellationToken cancellationToken);

    Task<ProfileTransferFile?> PickTransferAsync(string title, CancellationToken cancellationToken);
}
