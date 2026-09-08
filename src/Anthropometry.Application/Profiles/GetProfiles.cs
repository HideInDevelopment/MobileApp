using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Common;
using Anthropometry.Domain.Common;

namespace Anthropometry.Application.Profiles;

public sealed class GetProfiles
{
    private readonly IProfileRepository _repository;

    public GetProfiles(IProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<ProfileDto>>> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            var profiles = await _repository.GetAllAsync(cancellationToken);
            return Result.Success<IReadOnlyList<ProfileDto>>(profiles.Select(ApplicationModels.ToDto).ToArray());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Result.Failure<IReadOnlyList<ProfileDto>>(ApplicationErrors.PersistenceUnavailable);
        }
    }
}
