using Anthropometry.Application.Profiles;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Abstractions;

public interface IProfileTransferRepository
{
    Task<ProfileTransferSnapshot?> GetSnapshotAsync(ProfileId profileId, CancellationToken cancellationToken);

    Task ImportAsync(
        Profile profile,
        IReadOnlyList<Measurement> measurements,
        IReadOnlyList<CalculationResult> results,
        CancellationToken cancellationToken);
}
