namespace Anthropometry.Domain.Calculations.Tdee;

public sealed record ActivityFactorDefinition(
    ActivityLevel Level,
    string Label,
    string Description,
    decimal Factor);

public static class ActivityFactorTable
{
    public static bool TryGet(ActivityLevel level, out ActivityFactorDefinition? definition)
    {
        definition = level switch
        {
            ActivityLevel.Sedentary => new(ActivityLevel.Sedentary, "Sedentary", "Little or no exercise", 1.2m),
            ActivityLevel.Light => new(ActivityLevel.Light, "Light", "Light exercise one to three days per week", 1.375m),
            ActivityLevel.Moderate => new(ActivityLevel.Moderate, "Moderate", "Moderate exercise three to five days per week", 1.55m),
            ActivityLevel.High => new(ActivityLevel.High, "High", "Hard exercise six to seven days per week", 1.725m),
            ActivityLevel.VeryHigh => new(ActivityLevel.VeryHigh, "Very high", "Very hard exercise or a physical job", 1.9m),
            _ => null
        };

        return definition is not null;
    }
}
