using System.Text.Json;
using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Entitlements;
using Microsoft.Maui.Storage;

namespace Anthropometry.App.Entitlements;

public sealed class PreferencesEntitlementCache : IEntitlementCache
{
    private const string PreferenceKey = "premium.entitlement.v1";
    private const int CurrentVersion = 1;

    public EntitlementSnapshot? Load()
    {
        var json = Preferences.Default.Get(PreferenceKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<CacheEnvelope>(json);
            return envelope?.Version == CurrentVersion ? envelope.Snapshot : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void Save(EntitlementSnapshot snapshot)
    {
        var envelope = new CacheEnvelope(CurrentVersion, snapshot);
        Preferences.Default.Set(PreferenceKey, JsonSerializer.Serialize(envelope));
    }

    private sealed record CacheEnvelope(int Version, EntitlementSnapshot Snapshot);
}
