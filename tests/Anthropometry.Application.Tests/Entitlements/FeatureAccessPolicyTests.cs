using Anthropometry.Application.Entitlements;

namespace Anthropometry.Application.Tests.Entitlements;

public sealed class FeatureAccessPolicyTests
{
    [Fact]
    public void Free_access_is_limited_to_metric_core_features()
    {
        var free = new EntitlementSnapshot(
            EntitlementTier.Free,
            SubscriptionState.Active,
            null,
            null,
            null);

        Assert.Equal(1, FeatureAccessPolicy.GetMaximumProfiles(free));
        Assert.False(FeatureAccessPolicy.CanUse(free, PremiumFeature.PastMeasurements));
        Assert.False(FeatureAccessPolicy.CanUse(free, PremiumFeature.ImperialUnits));
        Assert.False(FeatureAccessPolicy.CanUse(free, PremiumFeature.EncryptedProfileTransfer));
        Assert.True(FeatureAccessPolicy.CanUse(free, PremiumFeature.KilogramWeightGraphic));
    }

    [Fact]
    public void Premium_access_allows_all_approved_features()
    {
        var premium = new EntitlementSnapshot(
            EntitlementTier.Premium,
            SubscriptionState.Active,
            "premium",
            DateTimeOffset.UtcNow.AddDays(30),
            DateTimeOffset.UtcNow);

        Assert.Equal(10, FeatureAccessPolicy.GetMaximumProfiles(premium));
        Assert.All(Enum.GetValues<PremiumFeature>(), feature =>
            Assert.True(FeatureAccessPolicy.CanUse(premium, feature)));
    }

    [Theory]
    [InlineData(SubscriptionState.Pending)]
    [InlineData(SubscriptionState.OnHold)]
    [InlineData(SubscriptionState.Expired)]
    [InlineData(SubscriptionState.Revoked)]
    public void Non_active_subscription_states_fail_closed(
        SubscriptionState state)
    {
        var snapshot = new EntitlementSnapshot(
            EntitlementTier.Premium,
            state,
            "premium",
            DateTimeOffset.UtcNow.AddDays(30),
            DateTimeOffset.UtcNow);

        Assert.False(FeatureAccessPolicy.CanUse(snapshot, PremiumFeature.ImperialUnits));
        Assert.Equal(1, FeatureAccessPolicy.GetMaximumProfiles(snapshot));
    }
}
