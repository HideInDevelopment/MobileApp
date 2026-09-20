namespace Anthropometry.Application.Entitlements;

public sealed record BillingOffer(
    string ProductId,
    string BasePlanId,
    string DisplayName,
    string PriceText);

public sealed record BillingCatalog(IReadOnlyList<BillingOffer> Offers);

public enum BillingPurchaseState
{
    Failed,
    Canceled,
    Pending,
    Purchased
}

public sealed record BillingPurchaseResult(
    BillingPurchaseState State,
    EntitlementSnapshot? Entitlement,
    string? ErrorMessage = null);
