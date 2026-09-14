using Anthropometry.Domain.Profiles;

namespace Anthropometry.App.Features.Profiles;

public sealed record GenderOption(ProfileGender Value, string DisplayName)
{
    public override string ToString() => DisplayName;
}
