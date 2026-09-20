namespace Anthropometry.Application.Entitlements;

public static class FeatureAccessPolicy
{
    public const int FreeMaximumProfiles = 1;

    public const int PremiumMaximumProfiles = 10;

    public static bool CanUse(EntitlementSnapshot entitlement, PremiumFeature feature)
    {
        if (feature == PremiumFeature.KilogramWeightGraphic)
        {
            return true;
        }

        return IsPremiumActive(entitlement);
    }

    public static int GetMaximumProfiles(EntitlementSnapshot entitlement)
        => IsPremiumActive(entitlement)
            ? PremiumMaximumProfiles
            : FreeMaximumProfiles;

    private static bool IsPremiumActive(EntitlementSnapshot entitlement)
        => entitlement.Tier == EntitlementTier.Premium
            && entitlement.State is SubscriptionState.Active
                or SubscriptionState.Canceled
                or SubscriptionState.InGracePeriod;
}
