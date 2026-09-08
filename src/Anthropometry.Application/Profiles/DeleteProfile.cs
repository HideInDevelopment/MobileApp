using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Common;
using Anthropometry.Domain.Common;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Profiles;

public sealed class DeleteProfile
{
    private readonly IProfileRepository _repository;

    public DeleteProfile(IProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> ExecuteAsync(ProfileId id, CancellationToken cancellationToken)
    {
        try
        {
            if (await _repository.GetByIdAsync(id, cancellationToken) is null)
            {
                return Result.Failure(ApplicationErrors.ProfileNotFound);
            }

            await _repository.DeleteWithMeasurementsAsync(id, cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Result.Failure(ApplicationErrors.PersistenceUnavailable);
        }
    }
}
