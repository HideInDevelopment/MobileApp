using Anthropometry.Application.Abstractions;

namespace Anthropometry.Application.Entitlements;

public sealed class FreeEntitlementProvider : IEntitlementProvider
{
    public static FreeEntitlementProvider Instance { get; } = new();

    private FreeEntitlementProvider()
    {
    }

    public Task<EntitlementSnapshot> GetCurrentAsync(CancellationToken cancellationToken)
        => Task.FromResult(new EntitlementSnapshot(
            EntitlementTier.Free,
            SubscriptionState.Active,
            null,
            null,
            null));
}
