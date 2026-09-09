using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Common;

namespace Anthropometry.Domain.Profiles;

public sealed record ProfileSettings(decimal HeightCm, int AgeYears, ActivityLevel ActivityLevel)
{
    public static Result<ProfileSettings> Create(decimal heightCm, int ageYears, ActivityLevel activityLevel)
    {
        if (heightCm is < 50m or > 300m)
        {
            return Result.Failure<ProfileSettings>(new DomainError("profile.settings.height.invalid", "Errors.ProfileSettingsHeightInvalid"));
        }

        if (ageYears is < 1 or > 120)
        {
            return Result.Failure<ProfileSettings>(new DomainError("profile.settings.age.invalid", "Errors.ProfileSettingsAgeInvalid"));
        }

        if (!Enum.IsDefined(activityLevel) || activityLevel == ActivityLevel.Unknown)
        {
            return Result.Failure<ProfileSettings>(new DomainError("profile.settings.activity.invalid", "Errors.ProfileSettingsActivityInvalid"));
        }

        return Result.Success(new ProfileSettings(heightCm, ageYears, activityLevel));
    }
}
