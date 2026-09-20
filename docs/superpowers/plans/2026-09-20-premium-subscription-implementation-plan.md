# Premium subscription and feature access Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (- [ ]) syntax for tracking.

**Goal:** Add a Google Play-only Premium subscription boundary that securely controls the approved Premium features while preserving the application's offline-first health-data workflow.

**Architecture:** Put deterministic entitlement and feature policy in Application, keep Domain independent of billing, and keep Android Google Play Billing behind an Application port implemented in the MAUI Android composition root. Verify Google Play purchases through a separate minimal ASP.NET Core billing service that stores billing identifiers and entitlement state but never receives health data. Cache the last verified entitlement locally for offline access without treating local flags as the authority.

**Tech Stack:** .NET 10, .NET MAUI Android, C#, XAML, CommunityToolkit.Mvvm, existing SQLite/Preferences infrastructure, xUnit, Microsoft-maintained Xamarin.Android.Google.BillingClient 9.1.0.2, ASP.NET Core Minimal API, Google Play Developer API client Google.Apis.AndroidPublisher.v3 1.76.0.4261, and Google Play test products.

**Spec:** docs/superpowers/specs/2026-09-20-premium-subscription-design.md

## Global Constraints

- Android is the only paid distribution target; the paid product is distributed through Google Play.
- Google Play Billing is the only in-app payment flow; the app must not expose a card, PayPal, or Stripe checkout.
- The first product is one Premium subscription with monthly and annual base plans; no trial is required.
- Free users have one profile; Premium users have up to ten profiles.
- Free users use metric display units and may view the kilogram weight graphic.
- Premium unlocks past-dated measurements, imperial display units, full measurement graphics, encrypted import/export, and additional profiles.
- Existing local data remains readable and recordable after Premium expires; entitlement loss never deletes data.
- Purchase verification and subscription lifecycle management belong to the billing boundary, not to the health-data service.
- The billing boundary must not receive profile names, measurements, calculation results, passphrases, or transfer contents.
- Domain code must not reference MAUI, Android, SQLite, XAML, Preferences, or billing SDK types.
- Application code must not execute SQL, reference MAUI controls, or reference Android billing types.
- Canonical persisted and formula units remain kilograms and centimeters; display conversion remains Presentation-owned.
- Measurement timestamps remain UTC in persistence; Presentation converts selected local dates for display and input.
- No credentials, Google service-account keys, purchase tokens, health data, or real Play product identifiers are committed.
- Every implementation task follows TDD: failing focused test, meaningful failure, minimal implementation, focused pass, applicable full-suite verification, and a focused commit.

## Review Focus

- Entitlement expiry while offline: Premium actions must fail closed after known expiry while existing data and Free workflows remain available; covered by Tasks 1 and 7.
- Import/export bypass: direct Application use-case calls must reject Free transfer operations even if UI gates are bypassed; covered by Task 2.
- Free users with a previously persisted imperial preference: all visible inputs, history, and charts must resolve to metric until Premium is verified; covered by Task 4.
- Past-measurement date boundaries: current local date is allowed for Free, past local dates require Premium, and future dates are always rejected; covered by Task 3.
- Pending or failed Play purchase verification: pending purchases must not unlock Premium and transient Play/backend failures must not clear local data; covered by Tasks 6 and 7.

## File Map

### Application and Domain

- Create src/Anthropometry.Application/Entitlements/EntitlementTier.cs for Free/Premium tiers.
- Create src/Anthropometry.Application/Entitlements/SubscriptionState.cs for normalized Play lifecycle states.
- Create src/Anthropometry.Application/Entitlements/PremiumFeature.cs for approved feature identifiers.
- Create src/Anthropometry.Application/Entitlements/EntitlementSnapshot.cs for verified entitlement state.
- Create src/Anthropometry.Application/Entitlements/FeatureAccessPolicy.cs for feature access and profile limits.
- Create src/Anthropometry.Application/Abstractions/IEntitlementProvider.cs for the Application entitlement port.
- Create src/Anthropometry.Application/Abstractions/IEntitlementCache.cs for the local snapshot cache port.
- Create src/Anthropometry.Application/Abstractions/IBillingGateway.cs for the platform billing port.
- Create src/Anthropometry.Application/Entitlements/EntitlementService.cs for cached refresh/restore orchestration.
- Modify src/Anthropometry.Application/Common/ApplicationErrors.cs for Premium-required and billing-state errors.
- Modify src/Anthropometry.Application/Profiles/CreateProfile.cs, ImportProfile.cs, and ExportProfile.cs to enforce feature access in use cases.
- Create src/Anthropometry.Application/Measurements/MeasurementDatePolicy.cs for current/past/future date validation.
- Modify src/Anthropometry.Application/Measurements/RecordMeasurement.cs and UpdateMeasurement.cs to enforce the date policy.

### Presentation

- Create src/Anthropometry.App/Entitlements/PreferencesEntitlementCache.cs for the local verified snapshot.
- Create src/Anthropometry.App/Features/Premium/PremiumPage.xaml, PremiumPage.xaml.cs, and PremiumViewModel.cs.
- Modify src/Anthropometry.App/Features/Settings/SettingsPage.xaml, SettingsPage.xaml.cs, and SettingsViewModel.cs for Premium and gated unit selection.
- Modify src/Anthropometry.App/Features/Profiles/ProfileListViewModel.cs and ProfileDetailViewModel.cs for locked profile/import/export actions.
- Modify src/Anthropometry.App/Features/Measurements/MeasurementEditorViewModel.cs and MeasurementEditorPage.xaml for a Premium-gated past date.
- Modify src/Anthropometry.App/Features/Measurements/WeightGraphicViewModel.cs and WeightGraphicPage.xaml for Free kilogram-only chart access.
- Modify profile, measurement history, editor, and chart Presentation consumers to use an effective metric/imperial display context.
- Create src/Anthropometry.App/Display/EntitledDisplayPreferences.cs to keep all Presentation unit consumers consistent.
- Modify src/Anthropometry.App/MauiNavigation.cs and MauiProgram.cs for Premium navigation and dependency composition.
- Modify all supported resource files under src/Anthropometry.App/Resources/Strings/ for localized Premium, billing, lock, date, and unit messages.

### Android billing and backend

- Create src/Anthropometry.App/Platforms/Android/AndroidPlayBillingGateway.cs implementing the Application billing port.
- Create src/Anthropometry.App/Platforms/Android/AndroidPlaySubscriptionLink.cs for Play subscription-management navigation.
- Modify src/Anthropometry.App/Anthropometry.App.csproj and Directory.Packages.props to add the pinned Android billing binding only to the Android target.
- Create services/Anthropometry.Billing/Anthropometry.Billing.csproj as an ASP.NET Core Minimal API.
- Create services/Anthropometry.Billing/Program.cs for verification endpoint and dependency composition.
- Create services/Anthropometry.Billing/GooglePlaySubscriptionVerifier.cs for Google Play Developer API calls.
- Create services/Anthropometry.Billing/BillingModels.cs for contracts containing no health data.
- Create services/Anthropometry.Billing/BillingStateStore.cs for minimal server-side entitlement state.
- Create services/Anthropometry.Billing/appsettings.example.json documenting environment-variable configuration without credentials.
- Add services/Anthropometry.Billing.Tests/ for verifier, endpoint, replay, and lifecycle tests.
- Modify Anthropometry.sln only after the standalone service builds and its tests pass.

### Tests

- Create tests/Anthropometry.Application.Tests/Entitlements/FeatureAccessPolicyTests.cs.
- Create tests/Anthropometry.Application.Tests/Entitlements/EntitlementServiceTests.cs.
- Modify tests/Anthropometry.Application.Tests/Profiles/ProfileUseCaseTests.cs and ProfileTransferUseCaseTests.cs.
- Create tests/Anthropometry.Application.Tests/Measurements/MeasurementDatePolicyTests.cs.
- Modify tests/Anthropometry.Application.Tests/Measurements/MeasurementUseCaseTests.cs and UpdateMeasurementTests.cs.
- Create tests/Anthropometry.App.Tests/Features/Premium/PremiumViewModelTests.cs and PremiumPageMarkupTests.cs.
- Modify settings, profile, measurement editor, chart, and transfer markup/ViewModel tests.
- Create tests/Anthropometry.App.Tests/Entitlements/EntitledDisplayPreferencesTests.cs.

---

### Task 1: Add the Application entitlement contract and deterministic feature policy

**Files:**
- Create: src/Anthropometry.Application/Entitlements/EntitlementTier.cs
- Create: src/Anthropometry.Application/Entitlements/SubscriptionState.cs
- Create: src/Anthropometry.Application/Entitlements/PremiumFeature.cs
- Create: src/Anthropometry.Application/Entitlements/EntitlementSnapshot.cs
- Create: src/Anthropometry.Application/Entitlements/FeatureAccessPolicy.cs
- Create: src/Anthropometry.Application/Abstractions/IEntitlementProvider.cs
- Modify: src/Anthropometry.Application/Common/ApplicationErrors.cs
- Test: tests/Anthropometry.Application.Tests/Entitlements/FeatureAccessPolicyTests.cs

**Interfaces:**

- IEntitlementProvider.GetCurrentAsync(CancellationToken) returns an EntitlementSnapshot.
- FeatureAccessPolicy.CanUse(EntitlementSnapshot, PremiumFeature) returns bool.
- FeatureAccessPolicy.GetMaximumProfiles(EntitlementSnapshot) returns 1 for Free and 10 for Premium.
- EntitlementSnapshot contains Tier, State, ProductId, ExpiresAtUtc, and LastVerifiedAtUtc.

- [ ] **Step 1: Write the failing Free/Premium matrix tests**

~~~csharp
[Fact]
public void Free_access_is_limited_to_metric_core_features()
{
    var policy = new FeatureAccessPolicy();
    var free = new EntitlementSnapshot(
        EntitlementTier.Free,
        SubscriptionState.Active,
        null,
        null,
        null);

    Assert.Equal(1, policy.GetMaximumProfiles(free));
    Assert.False(policy.CanUse(free, PremiumFeature.PastMeasurements));
    Assert.False(policy.CanUse(free, PremiumFeature.ImperialUnits));
    Assert.False(policy.CanUse(free, PremiumFeature.EncryptedProfileTransfer));
    Assert.True(policy.CanUse(free, PremiumFeature.KilogramWeightGraphic));
}

[Fact]
public void Premium_access_allows_all_approved_features()
{
    var policy = new FeatureAccessPolicy();
    var premium = new EntitlementSnapshot(
        EntitlementTier.Premium,
        SubscriptionState.Active,
        "premium",
        DateTimeOffset.UtcNow.AddDays(30),
        DateTimeOffset.UtcNow);

    Assert.Equal(10, policy.GetMaximumProfiles(premium));
    Assert.All(Enum.GetValues<PremiumFeature>(), feature =>
        Assert.True(policy.CanUse(premium, feature)));
}
~~~

- [ ] **Step 2: Run the focused tests and verify the meaningful failure**

Run:

~~~powershell
dotnet test tests/Anthropometry.Application.Tests/Anthropometry.Application.Tests.csproj --configuration Release --filter "FullyQualifiedName~FeatureAccessPolicyTests"
~~~

Expected: FAIL because the entitlement types and policy do not exist.

- [ ] **Step 3: Implement the minimal policy**

Use these stable feature identifiers:

~~~csharp
public enum PremiumFeature
{
    PastMeasurements,
    FullMeasurementGraphics,
    ImperialUnits,
    EncryptedProfileTransfer,
    AdditionalProfiles,
    KilogramWeightGraphic
}
~~~

The policy must treat KilogramWeightGraphic as Free and every other listed feature as Premium. It must return SubscriptionState.Pending, Expired, Revoked, and OnHold as Free access even if a stale tier says Premium.

- [ ] **Step 4: Run the focused tests and verify they pass**

Run the same command. Expected: PASS.

- [ ] **Step 5: Run the Application test project**

~~~powershell
dotnet test tests/Anthropometry.Application.Tests/Anthropometry.Application.Tests.csproj --configuration Release
~~~

Expected: all existing Application tests remain green.

- [ ] **Step 6: Commit**

~~~powershell
git add src/Anthropometry.Application/Entitlements src/Anthropometry.Application/Abstractions/IEntitlementProvider.cs src/Anthropometry.Application/Common/ApplicationErrors.cs tests/Anthropometry.Application.Tests/Entitlements/FeatureAccessPolicyTests.cs
git commit -m "feat: add premium entitlement policy"
~~~

### Task 2: Enforce profile, import, and export limits in Application use cases

**Files:**
- Modify: src/Anthropometry.Application/Profiles/CreateProfile.cs
- Modify: src/Anthropometry.Application/Profiles/ImportProfile.cs
- Modify: src/Anthropometry.Application/Profiles/ExportProfile.cs
- Modify: src/Anthropometry.Application/Common/ApplicationErrors.cs
- Modify: tests/Anthropometry.Application.Tests/Profiles/ProfileUseCaseTests.cs
- Modify: tests/Anthropometry.Application.Tests/Profiles/ProfileTransferUseCaseTests.cs
- Modify: tests/Anthropometry.Application.Tests/Support/Fakes.cs

**Interfaces:**

- CreateProfile, ImportProfile, and ExportProfile consume IEntitlementProvider and FeatureAccessPolicy.
- Free profile creation/import returns profile.limit.reached at one profile.
- Free import/export returns premium.feature.required, mapped to localized Presentation copy.

- [ ] **Step 1: Add failing tests for direct and bypass paths**

~~~csharp
[Fact]
public async Task Free_user_cannot_create_a_second_profile()
{
    var repository = new FakeProfileRepository { Items = [TestData.Profile("Existing")] };
    var useCase = new CreateProfile(repository, new FakeClock(), new FreeEntitlementProvider(), new FeatureAccessPolicy());

    var result = await useCase.ExecuteAsync(TestData.CreateProfileCommand("Second"), CancellationToken.None);

    Assert.False(result.IsSuccess);
    Assert.Equal("profile.limit.reached", result.Error!.Code);
}

[Fact]
public async Task Free_user_cannot_export_through_the_application_use_case()
{
    var useCase = new ExportProfile(
        new FakeProfileTransferRepository(TestData.Profile("Local")),
        new FakeClock(),
        new FreeEntitlementProvider(),
        new FeatureAccessPolicy());

    var result = await useCase.ExecuteAsync(
        new ExportProfileCommand(TestData.ProfileId),
        CancellationToken.None);

    Assert.False(result.IsSuccess);
    Assert.Equal("premium.feature.required", result.Error!.Code);
}
~~~

- [ ] **Step 2: Run the focused tests and verify the meaningful failure**

~~~powershell
dotnet test tests/Anthropometry.Application.Tests/Anthropometry.Application.Tests.csproj --configuration Release --filter "FullyQualifiedName~ProfileUseCaseTests|FullyQualifiedName~ProfileTransferUseCaseTests"
~~~

Expected: FAIL because the use cases still hard-code four profiles and do not receive entitlement access.

- [ ] **Step 3: Replace hard-coded limits with the policy**

Before reading or writing transfer content, obtain the entitlement snapshot and call CanUse. In CreateProfile and ImportProfile, compare repository count with GetMaximumProfiles. Preserve existing transaction and validation behavior.

- [ ] **Step 4: Run focused tests and verify Free/Premium paths**

Run the same command. Expected: PASS, including Premium creation/import through the ten-profile boundary and Free rejection at profile two.

- [ ] **Step 5: Run profile and transfer test groups**

~~~powershell
dotnet test tests/Anthropometry.Application.Tests/Anthropometry.Application.Tests.csproj --configuration Release --filter "FullyQualifiedName~Profiles"
~~~

Expected: PASS.

- [ ] **Step 6: Commit**

~~~powershell
git add src/Anthropometry.Application/Profiles src/Anthropometry.Application/Common/ApplicationErrors.cs tests/Anthropometry.Application.Tests/Profiles tests/Anthropometry.Application.Tests/Support/Fakes.cs
git commit -m "feat: enforce premium profile and transfer access"
~~~

### Task 3: Add past-measurement date validation and the Premium-gated date picker

**Files:**
- Create: src/Anthropometry.Application/Measurements/MeasurementDatePolicy.cs
- Modify: src/Anthropometry.Application/Measurements/RecordMeasurement.cs
- Modify: src/Anthropometry.Application/Measurements/UpdateMeasurement.cs
- Modify: src/Anthropometry.Application/Common/ApplicationErrors.cs
- Modify: src/Anthropometry.App/Features/Measurements/MeasurementEditorViewModel.cs
- Modify: src/Anthropometry.App/Features/Measurements/MeasurementEditorPage.xaml
- Modify: all supported files under src/Anthropometry.App/Resources/Strings/
- Create: tests/Anthropometry.Application.Tests/Measurements/MeasurementDatePolicyTests.cs
- Modify: tests/Anthropometry.Application.Tests/Measurements/MeasurementUseCaseTests.cs
- Modify: tests/Anthropometry.Application.Tests/Measurements/UpdateMeasurementTests.cs
- Modify: tests/Anthropometry.App.Tests/Features/Measurements/MeasurementEditorViewModelTests.cs
- Modify: tests/Anthropometry.App.Tests/Features/Measurements/MeasurementEditorPageMarkupTests.cs

**Interfaces:**

- MeasurementDatePolicy.Validate(DateTimeOffset measuredAtUtc, EntitlementSnapshot entitlement, DateTimeOffset nowUtc) returns Result.
- Free users may record only the current local calendar date.
- Premium users may record current or past local dates.
- No user may record a future local date.
- Existing MeasuredAtUtc storage remains unchanged; no schema migration is required.

- [ ] **Step 1: Write failing date-policy tests**

~~~csharp
[Fact]
public void Free_rejects_a_past_local_date()
{
    var now = new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);
    var measured = now.AddDays(-1);

    var result = new MeasurementDatePolicy().Validate(measured, Free(), now);

    Assert.False(result.IsSuccess);
    Assert.Equal("measurement.pastDate.premiumRequired", result.Error!.Code);
}

[Fact]
public void Premium_accepts_a_past_date_but_rejects_a_future_date()
{
    var now = new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);
    var policy = new MeasurementDatePolicy();

    Assert.True(policy.Validate(now.AddDays(-1), Premium(), now).IsSuccess);
    Assert.False(policy.Validate(now.AddDays(1), Premium(), now).IsSuccess);
}
~~~

- [ ] **Step 2: Run the focused tests and verify failure**

~~~powershell
dotnet test tests/Anthropometry.Application.Tests/Anthropometry.Application.Tests.csproj --configuration Release --filter "FullyQualifiedName~MeasurementDatePolicyTests"
~~~

Expected: FAIL because the policy does not exist.

- [ ] **Step 3: Implement Application validation**

Compare measuredAtUtc.ToLocalTime().Date with nowUtc.ToLocalTime().Date. Return a Premium-required error for Free past dates and a date-invalid error for future dates. Invoke the policy from both record and update use cases before persistence or result deletion. An edit that preserves an already-past measurement date remains possible; changing a current measurement to a past date requires Premium.

- [ ] **Step 4: Add the date picker and ViewModel state**

Add a MeasurementDate DateTime property initialized from the existing measurement or today. Convert the selected local date to a UTC timestamp at local noon before creating RecordMeasurementCommand. Keep the existing measurement timestamp when editing unless the Premium user changes it. Disable the picker for Free users and expose a localized Premium lock action.

- [ ] **Step 5: Run Application and App focused tests**

~~~powershell
dotnet test tests/Anthropometry.Application.Tests/Anthropometry.Application.Tests.csproj --configuration Release --filter "FullyQualifiedName~Measurement"
dotnet test tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~MeasurementEditor"
~~~

Expected: PASS, including date-picker markup and Free/Premium ViewModel state.

- [ ] **Step 6: Commit**

~~~powershell
git add src/Anthropometry.Application/Measurements src/Anthropometry.Application/Common/ApplicationErrors.cs src/Anthropometry.App/Features/Measurements src/Anthropometry.App/Resources/Strings tests/Anthropometry.Application.Tests/Measurements tests/Anthropometry.App.Tests/Features/Measurements
git commit -m "feat: add premium past measurement dates"
~~~

### Task 4: Make display units entitlement-aware

**Files:**
- Create: src/Anthropometry.App/Display/EntitledDisplayPreferences.cs
- Modify: src/Anthropometry.App/Display/DisplayPreferencesService.cs only where needed to expose canonical conversion helpers
- Modify: src/Anthropometry.App/Features/Profiles/ProfileEditorViewModel.cs
- Modify: src/Anthropometry.App/Features/Measurements/MeasurementEditorViewModel.cs
- Modify: src/Anthropometry.App/Features/Measurements/MeasurementHistoryViewModel.cs
- Modify: src/Anthropometry.App/Features/Measurements/WeightGraphicViewModel.cs
- Modify: src/Anthropometry.App/Features/Settings/SettingsViewModel.cs
- Modify: src/Anthropometry.App/Features/Settings/SettingsPage.xaml
- Modify: all supported files under src/Anthropometry.App/Resources/Strings/
- Create: tests/Anthropometry.App.Tests/Entitlements/EntitledDisplayPreferencesTests.cs
- Modify: tests/Anthropometry.App.Tests/Display/DisplayPreferencesServiceTests.cs
- Modify: tests/Anthropometry.App.Tests/Features/Settings/SettingsViewModelTests.cs
- Modify: tests/Anthropometry.App.Tests/Features/Profiles/ProfileEditorViewModelTests.cs

**Interfaces:**

- EntitledDisplayPreferences.MeasurementSystemCode returns metric for Free and the persisted metric/imperial choice for Premium.
- EntitledDisplayPreferences.WeightUnitCode, HeightUnitCode, and CircumferenceUnitCode derive from the effective system.
- SettingsViewModel exposes IsMeasurementSystemLocked and routes an imperial selection to Premium navigation without changing the preference for Free users.

- [ ] **Step 1: Write failing effective-unit tests**

~~~csharp
[Fact]
public void Free_users_are_always_presented_in_metric_units()
{
    var display = TestDisplayPreferences.WithMeasurementSystem(DisplayPreferencesService.ImperialCode);
    var entitled = new EntitledDisplayPreferences(display, FreeEntitlement());

    Assert.Equal(DisplayPreferencesService.MetricCode, entitled.MeasurementSystemCode);
    Assert.Equal(DisplayPreferencesService.KilogramsCode, entitled.WeightUnitCode);
    Assert.Equal(DisplayPreferencesService.MetersCode, entitled.HeightUnitCode);
    Assert.Equal(DisplayPreferencesService.CentimetersCode, entitled.CircumferenceUnitCode);
}
~~~

- [ ] **Step 2: Run focused tests and verify failure**

~~~powershell
dotnet test tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~EntitledDisplayPreferences"
~~~

Expected: FAIL because the entitlement-aware display context does not exist.

- [ ] **Step 3: Implement the effective display context**

Keep stored preference and conversion constants intact. Make all Presentation consumers use the effective context so a stale imperial preference cannot leak into inputs, history, or charts for Free users. Preserve the stored preference for later Premium restoration, but do not apply it until Premium is verified.

- [ ] **Step 4: Add the locked Settings interaction**

Leave the unit selector visible with a localized Premium label. Metric remains selectable for Free; selecting Imperial opens the Premium screen and leaves the effective system metric. Premium can switch and persist both systems.

- [ ] **Step 5: Run display, settings, profile, measurement, and chart tests**

~~~powershell
dotnet test tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~Display|FullyQualifiedName~Settings|FullyQualifiedName~ProfileEditor|FullyQualifiedName~MeasurementEditor|FullyQualifiedName~WeightGraphic"
~~~

Expected: PASS, with no conversion regressions.

- [ ] **Step 6: Commit**

~~~powershell
git add src/Anthropometry.App/Display src/Anthropometry.App/Features/Profiles/ProfileEditorViewModel.cs src/Anthropometry.App/Features/Measurements src/Anthropometry.App/Features/Settings src/Anthropometry.App/Resources/Strings tests/Anthropometry.App.Tests/Entitlements tests/Anthropometry.App.Tests/Display tests/Anthropometry.App.Tests/Features/Settings tests/Anthropometry.App.Tests/Features/Profiles
git commit -m "feat: gate imperial display units behind premium"
~~~

### Task 5: Limit the chart to Free kilogram weight data and unlock full Premium graphics

**Files:**
- Modify: src/Anthropometry.App/Features/Measurements/WeightGraphicViewModel.cs
- Modify: src/Anthropometry.App/Features/Measurements/WeightGraphicPage.xaml
- Modify: src/Anthropometry.App/Features/Measurements/IMeasurementNavigation.cs if Premium navigation is needed
- Modify: src/Anthropometry.App/MauiNavigation.cs
- Modify: tests/Anthropometry.App.Tests/Features/Measurements/WeightGraphicViewModelTests.cs
- Modify: tests/Anthropometry.App.Tests/Features/Measurements/MetricChartPageMarkupTests.cs

**Interfaces:**

- Free MetricOptions contains only MetricKind.Weight; point values and labels use kilograms regardless of a stale stored preference.
- Premium MetricOptions contains Weight, Body Fat, BMR, and TDEE and follows the effective display unit context.
- Selecting a locked chart option opens Premium rather than querying unauthorized data.

- [ ] **Step 1: Add failing chart tests**

~~~csharp
[Fact]
public void Free_chart_exposes_only_the_kilogram_weight_series()
{
    var viewModel = CreateViewModel(FreeEntitlement());

    Assert.Single(viewModel.MetricOptions);
    Assert.Equal(MetricKind.Weight, viewModel.MetricOptions[0].Value);
    Assert.Equal("Kg", viewModel.Points[0].Unit);
}
~~~

- [ ] **Step 2: Run focused chart tests and verify failure**

~~~powershell
dotnet test tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~WeightGraphicViewModelTests|FullyQualifiedName~MetricChartPageMarkupTests"
~~~

Expected: FAIL because the chart currently exposes every metric and follows the unguarded display preference.

- [ ] **Step 3: Implement the chart gate**

Build options from the current entitlement. Keep existing history query and drawing code unchanged for authorized metrics. On Free, force MetricKind.Weight, format values from canonical kilograms, and expose a localized locked Premium affordance for omitted options if the UI presents them.

- [ ] **Step 4: Run focused chart and full App tests**

~~~powershell
dotnet test tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~WeightGraphic"
dotnet test tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj --configuration Release
~~~

Expected: PASS.

- [ ] **Step 5: Commit**

~~~powershell
git add src/Anthropometry.App/Features/Measurements src/Anthropometry.App/MauiNavigation.cs tests/Anthropometry.App.Tests/Features/Measurements
git commit -m "feat: limit free charts to kilogram weight"
~~~

### Task 6: Add Premium Settings, locked-feature UX, and local entitlement caching

**Files:**
- Create: src/Anthropometry.Application/Abstractions/IEntitlementCache.cs
- Create: src/Anthropometry.Application/Entitlements/EntitlementService.cs
- Create: src/Anthropometry.App/Entitlements/PreferencesEntitlementCache.cs
- Create: src/Anthropometry.App/Features/Premium/PremiumPage.xaml
- Create: src/Anthropometry.App/Features/Premium/PremiumPage.xaml.cs
- Create: src/Anthropometry.App/Features/Premium/PremiumViewModel.cs
- Modify: src/Anthropometry.App/Features/Settings/SettingsPage.xaml and SettingsViewModel.cs
- Modify: src/Anthropometry.App/Features/Profiles/ProfileListViewModel.cs and ProfileDetailViewModel.cs
- Modify: src/Anthropometry.App/MauiNavigation.cs and MauiProgram.cs
- Modify: all supported files under src/Anthropometry.App/Resources/Strings/
- Create: tests/Anthropometry.Application.Tests/Entitlements/EntitlementServiceTests.cs
- Create: tests/Anthropometry.App.Tests/Features/Premium/PremiumViewModelTests.cs and PremiumPageMarkupTests.cs
- Modify: settings, profile, and transfer markup tests

**Interfaces:**

- IEntitlementCache.Load() returns the last local EntitlementSnapshot?; Save persists only billing state.
- EntitlementService.GetCurrentAsync returns a cached usable snapshot or refreshes through IBillingGateway.
- PremiumViewModel exposes Tier, StateText, IsPremium, SubscribeCommand, RestoreCommand, and ManageSubscriptionCommand.

- [ ] **Step 1: Add failing cache and Premium-screen tests**

~~~csharp
[Fact]
public async Task Expired_cache_returns_free_access_without_deleting_local_data()
{
    var cache = new FakeEntitlementCache(ExpiredPremium());
    var service = new EntitlementService(cache, new FailingBillingGateway(), new FakeClock());

    var snapshot = await service.GetCurrentAsync(CancellationToken.None);

    Assert.Equal(EntitlementTier.Free, snapshot.Tier);
}
~~~

Add markup assertions for Premium status, subscribe, restore, and manage controls. Add ViewModel tests that billing errors produce recoverable localized messages and do not clear health-data repositories.

- [ ] **Step 2: Run focused tests and verify failure**

~~~powershell
dotnet test tests/Anthropometry.Application.Tests/Anthropometry.Application.Tests.csproj --configuration Release --filter "FullyQualifiedName~EntitlementService"
dotnet test tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~Premium|FullyQualifiedName~Settings|FullyQualifiedName~ProfileTransfer"
~~~

Expected: FAIL because cache, service, Premium page, and commands do not exist.

- [ ] **Step 3: Implement cache and service**

Persist a versioned JSON snapshot in Preferences under an app-specific key. Treat missing, malformed, pending, expired, revoked, on-hold, and stale snapshots as Free until billing refresh succeeds. Do not store purchase tokens in Preferences; token handling remains in the billing adapter.

- [ ] **Step 4: Implement Premium screen and locked actions**

Add a Premium section to Settings. Profile creation/import/export, unit selection, past-date selection, and locked chart actions navigate to the same Premium screen. Use localized copy and keep the main app usable when Play Billing is unavailable.

- [ ] **Step 5: Run focused and full Application/App tests**

~~~powershell
dotnet test tests/Anthropometry.Application.Tests/Anthropometry.Application.Tests.csproj --configuration Release
dotnet test tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj --configuration Release
~~~

Expected: PASS.

- [ ] **Step 6: Commit**

~~~powershell
git add src/Anthropometry.Application/Abstractions/IEntitlementCache.cs src/Anthropometry.Application/Entitlements src/Anthropometry.App/Entitlements src/Anthropometry.App/Features/Premium src/Anthropometry.App/Features/Settings src/Anthropometry.App/Features/Profiles src/Anthropometry.App/MauiNavigation.cs src/Anthropometry.App/MauiProgram.cs src/Anthropometry.App/Resources/Strings tests/Anthropometry.Application.Tests/Entitlements tests/Anthropometry.App.Tests/Features/Premium tests/Anthropometry.App.Tests/Features/Settings tests/Anthropometry.App.Tests/Features/Profiles
git commit -m "feat: add premium settings and entitlement cache"
~~~

### Task 7: Add the Android Google Play Billing adapter

**Files:**
- Create: src/Anthropometry.Application/Abstractions/IBillingGateway.cs
- Create: src/Anthropometry.Application/Entitlements/BillingModels.cs
- Modify: Directory.Packages.props
- Modify: src/Anthropometry.App/Anthropometry.App.csproj
- Create: src/Anthropometry.App/Platforms/Android/AndroidPlayBillingGateway.cs
- Create: src/Anthropometry.App/Platforms/Android/AndroidPlaySubscriptionLink.cs
- Modify: src/Anthropometry.App/MauiProgram.cs
- Modify: all supported files under src/Anthropometry.App/Resources/Strings/
- Create: tests/Anthropometry.App.Tests/Entitlements/BillingContractTests.cs

**Interfaces:**

- IBillingGateway.GetCatalogAsync(CancellationToken) returns a product with monthly and annual base-plan offers.
- IBillingGateway.PurchaseAsync(string productId, string basePlanId, CancellationToken) returns a normalized purchase result.
- IBillingGateway.RestoreAsync(CancellationToken) returns a normalized entitlement snapshot.
- IBillingGateway.OpenManageSubscription() launches the Play subscription-management URI without returning purchase data.

- [ ] **Step 1: Add failing billing contract tests**

Test pending purchase maps to SubscriptionState.Pending, canceled purchases do not become Premium after expiration, and a successful normalized result includes product/base-plan identity without exposing a raw token to Presentation.

- [ ] **Step 2: Run focused tests and verify failure**

~~~powershell
dotnet test tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~BillingContractTests"
~~~

Expected: FAIL because the billing port and normalized models do not exist.

- [ ] **Step 3: Add and pin the official .NET Android binding**

Add the Microsoft-maintained binding to the Android target only:

~~~xml
<PackageVersion Include="Xamarin.Android.Google.BillingClient" Version="9.1.0.2" />
~~~

Use the Play Billing lifecycle: connect, query ProductDetails, launch purchase flow, process purchase updates, query existing subscriptions for restore, and acknowledge completed purchases. Never expose PurchaseToken beyond the adapter/verifier boundary.

- [ ] **Step 4: Implement the Android adapter and manage link**

Use ANDROID-only composition. Map Play states into Application SubscriptionState. Treat BillingClient unavailable, no product, user cancellation, pending purchase, and connection failure as recoverable results. Build the specific Play subscription-management URI from package name and product ID.

- [ ] **Step 5: Build Android and run all non-Android tests**

~~~powershell
dotnet build .\\src\\Anthropometry.App\\Anthropometry.App.csproj --configuration Release --framework net10.0-android
dotnet test Anthropometry.sln --configuration Release --no-restore
~~~

Expected: Android build and all tests pass. No Play credentials are needed for compilation.

- [ ] **Step 6: Commit**

~~~powershell
git add Directory.Packages.props src/Anthropometry.Application/Abstractions/IBillingGateway.cs src/Anthropometry.Application/Entitlements src/Anthropometry.App/Anthropometry.App.csproj src/Anthropometry.App/Platforms/Android src/Anthropometry.App/MauiProgram.cs src/Anthropometry.App/Resources/Strings tests/Anthropometry.App.Tests/Entitlements
git commit -m "feat: integrate Google Play billing boundary"
~~~

### Task 8: Implement the minimal purchase-verification service

**Files:**
- Create: services/Anthropometry.Billing/Anthropometry.Billing.csproj
- Create: services/Anthropometry.Billing/Program.cs
- Create: services/Anthropometry.Billing/BillingModels.cs
- Create: services/Anthropometry.Billing/GooglePlaySubscriptionVerifier.cs
- Create: services/Anthropometry.Billing/BillingStateStore.cs
- Create: services/Anthropometry.Billing/appsettings.example.json
- Create: services/Anthropometry.Billing.Tests/Anthropometry.Billing.Tests.csproj
- Create: services/Anthropometry.Billing.Tests/GooglePlaySubscriptionVerifierTests.cs
- Create: services/Anthropometry.Billing.Tests/BillingEndpointTests.cs
- Create: services/Anthropometry.Billing.Tests/BillingStateStoreTests.cs
- Modify: Directory.Packages.props
- Modify: Anthropometry.sln

**Interfaces:**

- POST /v1/google-play/entitlements accepts packageName, productId, and purchaseToken.
- The endpoint returns tier, state, productId, expiresAtUtc, and lastVerifiedAtUtc.
- IGooglePlaySubscriptionVerifier.VerifyAsync(packageName, productId, purchaseToken, CancellationToken) returns normalized entitlement.
- The service persists purchase-token hash, package name, product ID, state, expiry, and verification time; it never persists raw token or health data.

- [ ] **Step 1: Add failing service tests**

~~~csharp
[Fact]
public async Task Active_google_purchase_returns_premium_without_health_data_fields()
{
    var verifier = new FakeGooglePlayVerifier(ActivePurchase());
    var client = CreateTestClient(verifier);

    var response = await client.PostAsJsonAsync(
        "/v1/google-play/entitlements",
        new { packageName = "com.companyname.anthropometry.app", productId = "premium", purchaseToken = "test-token" });

    response.EnsureSuccessStatusCode();
    var body = await response.Content.ReadFromJsonAsync<JsonObject>();
    Assert.Equal("Premium", body!["tier"]!.GetValue<string>());
    Assert.DoesNotContain("profile", response.Content.ReadAsStringAsync().Result, StringComparison.OrdinalIgnoreCase);
}
~~~

Add tests for malformed requests, wrong package/product, expired/revoked purchases, replayed tokens, and Google API failure.

- [ ] **Step 2: Run service tests and verify failure**

~~~powershell
dotnet test services/Anthropometry.Billing.Tests/Anthropometry.Billing.Tests.csproj --configuration Release
~~~

Expected: FAIL because the service project and endpoint do not exist.

- [ ] **Step 3: Create the service with pinned Google API client**

Add:

~~~xml
<PackageVersion Include="Google.Apis.AndroidPublisher.v3" Version="1.76.0.4261" />
~~~

Use service-account credentials only through an environment-provided file path or workload identity configuration. The endpoint calls the Google Play Developer API to verify subscription state and expiry, acknowledges valid purchases, and maps lifecycle states into the Application contract.

- [ ] **Step 4: Add replay-safe state handling**

Hash purchase tokens before persistence. Reject a token whose package/product context does not match the configured application. Make repeated verification idempotent. Return only normalized entitlement state. Use a bounded request body and rate-limit the endpoint at the hosting boundary.

- [ ] **Step 5: Run service tests and build**

~~~powershell
dotnet test services/Anthropometry.Billing.Tests/Anthropometry.Billing.Tests.csproj --configuration Release
dotnet build services/Anthropometry.Billing/Anthropometry.Billing.csproj --configuration Release
~~~

Expected: PASS without credentials by using the fake verifier in tests.

- [ ] **Step 6: Commit**

~~~powershell
git add services Directory.Packages.props Anthropometry.sln
git commit -m "feat: add Google Play entitlement verification service"
~~~

### Task 9: Connect the Android adapter to verification and complete composition

**Files:**
- Modify: src/Anthropometry.App/Platforms/Android/AndroidPlayBillingGateway.cs
- Modify: src/Anthropometry.App/Features/Premium/PremiumViewModel.cs
- Modify: src/Anthropometry.Application/Entitlements/EntitlementService.cs
- Modify: src/Anthropometry.App/MauiProgram.cs
- Modify: all supported files under src/Anthropometry.App/Resources/Strings/
- Modify: tests/Anthropometry.Application.Tests/Entitlements/EntitlementServiceTests.cs
- Modify: tests/Anthropometry.App.Tests/Features/Premium/PremiumViewModelTests.cs

**Interfaces:**

- A completed Play purchase sends its token only to the configured verification endpoint.
- The returned verified snapshot is saved through IEntitlementCache.
- Restore uses Play's purchase query, verifies each relevant purchase, and updates the cache.
- Subscription-management navigation uses Play's URL and never attempts to reproduce payment UI.

- [ ] **Step 1: Add failing orchestration tests**

~~~csharp
[Fact]
public async Task Successful_purchase_saves_verified_premium_snapshot()
{
    var cache = new FakeEntitlementCache();
    var billing = new FakeBillingGateway(VerifiedPremium());
    var service = new EntitlementService(cache, billing, new FakeClock());

    var result = await service.PurchaseAsync("premium", "annual", CancellationToken.None);

    Assert.Equal(EntitlementTier.Premium, result.Tier);
    Assert.Equal(result, cache.Saved);
}
~~~

Also test that verification failure leaves the previous cache untouched and pending purchases leave the tier Free.

- [ ] **Step 2: Run focused tests and verify failure**

~~~powershell
dotnet test tests/Anthropometry.Application.Tests/Anthropometry.Application.Tests.csproj --configuration Release --filter "FullyQualifiedName~EntitlementServiceTests"
dotnet test tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~PremiumViewModelTests"
~~~

Expected: FAIL because purchase/restore orchestration is not connected.

- [ ] **Step 3: Implement verification orchestration**

On purchase completion, ignore local purchase state until the billing gateway returns a verified snapshot. Save only that snapshot. On restore, query Play, verify each purchase, choose the active Premium entitlement with the furthest valid expiry, and retain Free when none is valid.

- [ ] **Step 4: Wire production composition**

Register entitlement cache, entitlement service, Android billing gateway, Premium ViewModel/page, and updated use cases. Configure the verification endpoint through a non-secret build configuration value. Do not add the endpoint URL to test fixtures or commit production credentials.

- [ ] **Step 5: Run the full applicable suite**

~~~powershell
dotnet restore
dotnet build Anthropometry.sln --configuration Release
dotnet test Anthropometry.sln --configuration Release
dotnet build .\\src\\Anthropometry.App\\Anthropometry.App.csproj --configuration Release --framework net10.0-android
~~~

Expected: all tests and the Android build pass.

- [ ] **Step 6: Commit**

~~~powershell
git add src services tests
git commit -m "feat: connect premium purchase verification"
~~~

### Task 10: Play test-product validation and release handoff

**Files:**
- Modify: README.md with Play Console setup and license-tester instructions.
- Modify: ARCHITECTURE.md to document the billing boundary and exception to the former no-network MVP boundary.
- Modify: PLAN.md or PLANV2.MD with the completed commercial slice and deferred work.
- Test: services/Anthropometry.Billing.Tests/ and Android manual validation.

- [ ] **Step 1: Configure Play test products outside source control**

Create the premium subscription in Play Console with monthly and annual base plans. Add license testers and internal-test access. Keep purchase identifiers in release configuration, not in tests or committed source.

- [ ] **Step 2: Validate critical flows on an internal Play build**

Verify:

1. Free user can create exactly one profile.
2. Free user can record today's measurement and view history/results.
3. Free user sees metric units and the kilogram weight chart.
4. Locked Premium actions open the Premium screen.
5. Successful monthly and annual purchases unlock Premium.
6. Restore on a second device using the same Google Play account restores Premium.
7. Pending purchase does not unlock Premium.
8. Cancellation retains access until expiry.
9. Expiration removes only new Premium actions.
10. Offline use preserves valid cached Premium access and never deletes local data.
11. Billing/backend failure leaves the core app usable.

- [ ] **Step 3: Run final verification**

~~~powershell
dotnet build Anthropometry.sln --configuration Release
dotnet test Anthropometry.sln --configuration Release
dotnet build .\\src\\Anthropometry.App\\Anthropometry.App.csproj --configuration Release --framework net10.0-android
git diff --check
git status --short
~~~

Expected: all commands pass, no credentials or personal data appear in the diff, and only intended commercial feature changes are present.

- [ ] **Step 4: Commit documentation and release handoff**

~~~powershell
git add README.md ARCHITECTURE.md PLAN.md PLANV2.MD
git commit -m "docs: document premium release and billing boundary"
~~~

## Final integration checklist

- [ ] Application tests cover every Premium feature and every denied Free path.
- [ ] Presentation never grants Premium based on a local UI flag.
- [ ] Import/export cannot bypass the entitlement policy through direct use-case calls.
- [ ] Unit conversion remains canonical in kilograms/centimeters for formulas and persistence.
- [ ] No health data crosses the billing boundary.
- [ ] Billing tokens and service-account credentials are absent from logs and Git.
- [ ] Android builds with the pinned Google Billing binding.
- [ ] The service verifies Google purchases through the Google Play Developer API.
- [ ] Subscription restore and Play management are reachable from Settings.
- [ ] Existing local data remains available after Premium expiration.
