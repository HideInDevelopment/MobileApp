using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Common;
using Anthropometry.Domain.Common;
using Anthropometry.Domain.Profiles;
using Anthropometry.Domain.Calculations;

namespace Anthropometry.Application.Profiles;

public sealed class CreateProfile
{
    private const int MaxProfiles = 4;
    private readonly IProfileRepository _repository;
    private readonly IClock _clock;

    public CreateProfile(IProfileRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<Result<ProfileDto>> ExecuteAsync(CreateProfileCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var profiles = await _repository.GetAllAsync(cancellationToken);
            if (profiles.Count >= MaxProfiles)
            {
                return Result.Failure<ProfileDto>(ApplicationErrors.ProfileLimitReached);
            }

            var settings = ProfileSettings.Create(command.Settings.HeightCm, command.Settings.AgeYears, command.Settings.ActivityLevel);
            if (!settings.IsSuccess)
            {
                return Result.Failure<ProfileDto>(settings.Error!);
            }

            var created = Profile.Create(command.Name, settings.Value, _clock.UtcNow, command.Gender);
            if (!created.IsSuccess)
            {
                return Result.Failure<ProfileDto>(created.Error!);
            }

            await _repository.AddAsync(created.Value, cancellationToken);
            return Result.Success(ApplicationModels.ToDto(created.Value));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Result.Failure<ProfileDto>(ApplicationErrors.PersistenceUnavailable);
        }
    }
}

public sealed record ProfileSettingsInput(decimal HeightCm, int AgeYears, ActivityLevel ActivityLevel);

public sealed record CreateProfileCommand(
    string Name,
    ProfileSettingsInput Settings,
    ProfileGender Gender = ProfileGender.Male);
