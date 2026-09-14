using Anthropometry.Domain.Common;

namespace Anthropometry.Domain.Profiles;

public sealed class Profile
{
    private const int MaxNameLength = 100;

    private Profile(
        ProfileId id,
        string name,
        ProfileGender gender,
        ProfileSettings? settings,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        Name = name;
        Gender = gender;
        Settings = settings;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public ProfileId Id { get; }

    public string Name { get; private set; }

    public ProfileGender Gender { get; private set; }

    public ProfileSettings? Settings { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Result<Profile> Create(
        string? name,
        ProfileSettings? settings,
        DateTimeOffset createdAtUtc,
        ProfileGender gender = ProfileGender.Male)
    {
        var nameError = Guard.Required(name, "profile.name.required", "Errors.ProfileNameRequired", MaxNameLength);
        if (nameError is not null)
        {
            return Result.Failure<Profile>(nameError);
        }

        var timestampError = Guard.Utc(createdAtUtc, "profile.createdAtUtc.invalid", "Errors.ProfileCreatedAtUtcInvalid");
        if (timestampError is not null)
        {
            return Result.Failure<Profile>(timestampError);
        }

        if (settings is null)
        {
            return Result.Failure<Profile>(new DomainError("profile.settings.required", "Errors.ProfileSettingsRequired"));
        }

        var genderError = ValidateGender(gender);
        if (genderError is not null)
        {
            return Result.Failure<Profile>(genderError);
        }

        return Result.Success(new Profile(ProfileId.New(), name!.Trim(), gender, settings, createdAtUtc, createdAtUtc));
    }

    public static Result<Profile> Rehydrate(
        ProfileId id,
        string? name,
        ProfileSettings? settings,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        ProfileGender gender = ProfileGender.Male)
    {
        if (id.Value == Guid.Empty)
        {
            return Result.Failure<Profile>(new DomainError("profile.id.invalid", "Errors.ProfileIdInvalid"));
        }

        var created = Create(name, settings, createdAtUtc);
        if (!created.IsSuccess && settings is null && created.Error?.Code == "profile.settings.required")
        {
            var nameError = Guard.Required(name, "profile.name.required", "Errors.ProfileNameRequired", MaxNameLength);
            if (nameError is not null)
            {
                return Result.Failure<Profile>(nameError);
            }

            var legacyTimestampError = Guard.Utc(createdAtUtc, "profile.createdAtUtc.invalid", "Errors.ProfileCreatedAtUtcInvalid");
            if (legacyTimestampError is not null)
            {
                return Result.Failure<Profile>(legacyTimestampError);
            }

            created = Result.Success(new Profile(id, name!.Trim(), gender, null, createdAtUtc, createdAtUtc));
        }

        if (!created.IsSuccess)
        {
            return created;
        }

        var genderError = ValidateGender(gender);
        if (genderError is not null)
        {
            return Result.Failure<Profile>(genderError);
        }

        var timestampError = Guard.Utc(updatedAtUtc, "profile.updatedAtUtc.invalid", "Errors.ProfileUpdatedAtUtcInvalid");
        return timestampError is null
            ? Result.Success(new Profile(id, created.Value.Name, gender, created.Value.Settings, createdAtUtc, updatedAtUtc))
            : Result.Failure<Profile>(timestampError);
    }

    public Result Update(string? name, ProfileSettings? settings, DateTimeOffset updatedAtUtc)
        => Update(name, settings, updatedAtUtc, Gender);

    public Result Update(
        string? name,
        ProfileSettings? settings,
        DateTimeOffset updatedAtUtc,
        ProfileGender gender)
    {
        var nameError = Guard.Required(name, "profile.name.required", "Errors.ProfileNameRequired", MaxNameLength);
        if (nameError is not null)
        {
            return Result.Failure(nameError);
        }

        var timestampError = Guard.Utc(updatedAtUtc, "profile.updatedAtUtc.invalid", "Errors.ProfileUpdatedAtUtcInvalid");
        if (timestampError is not null)
        {
            return Result.Failure(timestampError);
        }

        if (settings is null)
        {
            return Result.Failure(new DomainError("profile.settings.required", "Errors.ProfileSettingsRequired"));
        }

        var genderError = ValidateGender(gender);
        if (genderError is not null)
        {
            return Result.Failure(genderError);
        }

        Name = name!.Trim();
        Gender = gender;
        Settings = settings;
        UpdatedAtUtc = updatedAtUtc;
        return Result.Success();
    }

    private static DomainError? ValidateGender(ProfileGender gender)
        => !Enum.IsDefined(gender) || gender == ProfileGender.Unknown
            ? new DomainError("profile.gender.invalid", "Errors.ProfileGenderInvalid")
            : null;
}
