using Anthropometry.Application.Abstractions;

namespace Anthropometry.Application.Entitlements;

public sealed class EntitlementService : IEntitlementProvider
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromDays(7);

    private readonly IEntitlementCache _cache;
    private readonly IBillingGateway _billing;
    private readonly IClock _clock;

    public EntitlementService(
        IEntitlementCache cache,
        IBillingGateway billing,
        IClock clock)
    {
        _cache = cache;
        _billing = billing;
        _clock = clock;
    }

    public async Task<EntitlementSnapshot> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var cached = _cache.Load();
        if (IsUsable(cached, now))
        {
            return cached!;
        }

        try
        {
            var refreshed = await _billing.RestoreAsync(cancellationToken);
            if (refreshed is not null)
            {
                _cache.Save(refreshed);
                if (IsUsable(refreshed, now))
                {
                    return refreshed;
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Billing is an optional refresh path. The app remains usable offline.
        }

        return FreeSnapshot(now);
    }

    public void StoreVerifiedSnapshot(EntitlementSnapshot snapshot)
        => _cache.Save(snapshot);

    public static bool IsUsable(EntitlementSnapshot? snapshot, DateTimeOffset nowUtc)
    {
        if (snapshot is null)
        {
            return false;
        }

        if (snapshot.Tier == EntitlementTier.Free)
        {
            return snapshot.State is SubscriptionState.Active or SubscriptionState.Expired;
        }

        if (snapshot.State is not (SubscriptionState.Active
            or SubscriptionState.Canceled
            or SubscriptionState.InGracePeriod))
        {
            return false;
        }

        if (snapshot.ExpiresAtUtc is { } expiresAtUtc && expiresAtUtc <= nowUtc)
        {
            return false;
        }

        return snapshot.LastVerifiedAtUtc is null
            || nowUtc - snapshot.LastVerifiedAtUtc <= CacheLifetime;
    }

    private static EntitlementSnapshot FreeSnapshot(DateTimeOffset nowUtc)
        => new(
            EntitlementTier.Free,
            SubscriptionState.Active,
            null,
            null,
            nowUtc);
}
