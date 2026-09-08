using Anthropometry.Domain.Common;

namespace Anthropometry.Domain.Profiles;

public sealed class Profile
{
    private const int MaxNameLength = 100;

    private Profile(ProfileId id, string name, DateTimeOffset createdAtUtc, DateTimeOffset updatedAtUtc)
    {
        Id = id;
        Name = name;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public ProfileId Id { get; }

    public string Name { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Result<Profile> Create(string? name, DateTimeOffset createdAtUtc)
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

        return Result.Success(new Profile(ProfileId.New(), name!.Trim(), createdAtUtc, createdAtUtc));
    }

    public static Result<Profile> Rehydrate(
        ProfileId id,
        string? name,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (id.Value == Guid.Empty)
        {
            return Result.Failure<Profile>(new DomainError("profile.id.invalid", "Errors.ProfileIdInvalid"));
        }

        var created = Create(name, createdAtUtc);
        if (!created.IsSuccess)
        {
            return created;
        }

        var timestampError = Guard.Utc(updatedAtUtc, "profile.updatedAtUtc.invalid", "Errors.ProfileUpdatedAtUtcInvalid");
        return timestampError is null
            ? Result.Success(new Profile(id, created.Value.Name, createdAtUtc, updatedAtUtc))
            : Result.Failure<Profile>(timestampError);
    }

    public Result Rename(string? name, DateTimeOffset updatedAtUtc)
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

        Name = name!.Trim();
        UpdatedAtUtc = updatedAtUtc;
        return Result.Success();
    }
}
