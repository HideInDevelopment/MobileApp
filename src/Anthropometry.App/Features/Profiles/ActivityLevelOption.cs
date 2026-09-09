using Anthropometry.Domain.Calculations;

namespace Anthropometry.App.Features.Profiles;

public sealed record ActivityLevelOption(ActivityLevel Value, string DisplayName)
{
    public override string ToString() => DisplayName;
}
