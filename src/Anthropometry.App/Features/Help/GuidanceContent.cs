using Anthropometry.App.Localization;

namespace Anthropometry.App.Features.Help;

public sealed record GuidanceContent(string Title, string Body)
{
    public static GuidanceContent Get(LanguageService languageService, GuidanceTopic topic)
    {
        ArgumentNullException.ThrowIfNull(languageService);

        var keys = topic switch
        {
            GuidanceTopic.ActivityLevel => ("GuidanceActivityLevelTitle", "GuidanceActivityLevelBody"),
            GuidanceTopic.Weight => ("GuidanceWeightTitle", "GuidanceWeightBody"),
            GuidanceTopic.Neck => ("GuidanceNeckTitle", "GuidanceNeckBody"),
            GuidanceTopic.Abdomen => ("GuidanceAbdomenTitle", "GuidanceAbdomenBody"),
            GuidanceTopic.Waist => ("GuidanceWaistTitle", "GuidanceWaistBody"),
            GuidanceTopic.Hip => ("GuidanceHipTitle", "GuidanceHipBody"),
            GuidanceTopic.BodyFat => ("GuidanceBodyFatTitle", "GuidanceBodyFatBody"),
            GuidanceTopic.Bmr => ("GuidanceBmrTitle", "GuidanceBmrBody"),
            GuidanceTopic.Tdee => ("GuidanceTdeeTitle", "GuidanceTdeeBody"),
            GuidanceTopic.ReusedSizes => ("GuidanceReusedSizesTitle", "GuidanceReusedSizesBody"),
            GuidanceTopic.EstimateDisclaimer => ("GuidanceEstimateDisclaimerTitle", "GuidanceEstimateDisclaimerBody"),
            _ => throw new ArgumentOutOfRangeException(nameof(topic), topic, "Unsupported guidance topic.")
        };

        return new(languageService.Get(keys.Item1), languageService.Get(keys.Item2));
    }
}
