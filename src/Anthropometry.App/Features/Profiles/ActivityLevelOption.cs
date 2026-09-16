using Anthropometry.Domain.Calculations;
using Anthropometry.App.Localization;

namespace Anthropometry.App.Features.Profiles;

public sealed record ActivityLevelOption(ActivityLevel Value, string DisplayName)
{
    public string Description { get; init; } = string.Empty;

    public static ActivityLevelOption Create(ActivityLevel value, LanguageService languageService)
    {
        ArgumentNullException.ThrowIfNull(languageService);

        var keys = value switch
        {
            ActivityLevel.Sedentary => ("Sedentary", "ActivityLevelSedentaryDescription"),
            ActivityLevel.Light => ("LightlyActive", "ActivityLevelLightDescription"),
            ActivityLevel.Moderate => ("ModeratelyActive", "ActivityLevelModerateDescription"),
            ActivityLevel.High => ("HighlyActive", "ActivityLevelHighDescription"),
            ActivityLevel.VeryHigh => ("VeryHighlyActive", "ActivityLevelVeryHighDescription"),
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported activity level.")
        };

        return new(value, languageService.Get(keys.Item1))
        {
            Description = languageService.Get(keys.Item2)
        };
    }

    public override string ToString() => DisplayName;
}
