using Anthropometry.Application.Entitlements;

namespace Anthropometry.Application.Abstractions;

public interface IEntitlementCache
{
    EntitlementSnapshot? Load();

    void Save(EntitlementSnapshot snapshot);
}
