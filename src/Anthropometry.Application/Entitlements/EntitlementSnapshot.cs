namespace Anthropometry.Application.Entitlements;

public sealed record EntitlementSnapshot(
    EntitlementTier Tier,
    SubscriptionState State,
    string? ProductId,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? LastVerifiedAtUtc);
