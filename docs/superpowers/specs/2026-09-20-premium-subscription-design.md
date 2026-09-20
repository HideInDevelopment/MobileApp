# Premium subscription and feature access design

## Status

Approved conversational design; pending user review before implementation planning.

## Context and decisions

Anthropometry is currently an Android-first, local-first application. Profiles, measurements, calculation results, settings, encrypted profile transfer, and reminders are stored or scheduled locally. The current application has no account system, network service, analytics SDK, or synchronization boundary.

The paid version will initially be distributed through Google Play only. Digital functionality sold inside a Play-distributed application must use Google Play Billing unless a documented policy exception applies. Google Play also recommends a secure backend for purchase verification and subscription-specific entitlement management.

Sources:

- [Google Play payments policy](https://support.google.com/googleplay/android-developer/answer/10281818)
- [Google Play Billing overview](https://developer.android.com/google/play/billing)
- [Google Play Billing integration flow](https://developer.android.com/google/play/billing/integrate)
- [Subscription lifecycle and management](https://developer.android.com/google/play/billing/subscriptions)
- [Billing backend integration](https://developer.android.com/google/play/billing/backend)

The first commercial product is one Premium subscription. The application will not expose its own card, PayPal, or Stripe checkout. Google Play owns the payment UI, supported payment methods, renewals, refunds, cancellations, and subscription-management experience.

The recommended initial catalog is one subscription product with monthly and annual base plans. No trial or promotional offer is required for the first implementation; those can be added later through Play Console without changing the feature policy.

## Product tiers

### Free

- One profile.
- Current-date measurements.
- Calculation results and measurement history.
- Weight graphic in kilograms.
- English and other supported languages.
- Date-format preference.
- Light/dark theme preference.
- Metric display units only.
- No encrypted profile import/export.

### Premium

- Up to ten profiles.
- Measurements dated in the past.
- Full measurement graphics and all supported graphic units/series.
- Metric or imperial measurement display units.
- Encrypted profile export.
- Encrypted profile import.
- All Free functionality.

The application must never delete, hide, or make existing local measurements inaccessible because Premium expires. Expiration only prevents new Premium actions. The core measurement, history, and calculation workflow remains available to Free users.

There are currently no users with more than one profile, so the new Free limit can be applied directly. A future migration must still preserve all local data if the rule changes after release.

Locked Premium controls remain visible with a localized Premium indicator. Selecting a locked control opens the Premium screen rather than producing a generic validation error. The UI is not the authority for access: the same entitlement rule is enforced by Application use cases.

## Architecture

### Entitlement model

Add an Application-owned entitlement model with no MAUI, Android, SQLite, or payment-SDK references:

- `EntitlementTier`: `Free` or `Premium`.
- `PremiumFeature`: `PastMeasurements`, `FullMeasurementGraphics`, `ImperialUnits`, `EncryptedProfileTransfer`, and `AdditionalProfiles`.
- `EntitlementSnapshot`: tier, subscription state, product/base-plan identity, expiration time when known, and last verification time.
- `FeatureAccessPolicy`: maps an entitlement snapshot to feature access and profile limits.

The policy must be deterministic and independently testable. It owns the one-profile Free limit and ten-profile Premium limit. `CreateProfile` and `ImportProfile` must consume this policy instead of maintaining their own hard-coded limits.

Existing core functionality must not depend on a billing client. Application use cases receive the current entitlement through an Application port or a small access service. This keeps tests deterministic and prevents payment concerns from leaking into Domain formulas or persistence repositories.

### Billing boundary

Presentation owns the Android billing adapter and purchase-flow launch. The adapter is responsible for:

1. Connecting to Google Play Billing.
2. Loading current subscription product details.
3. Launching the Google Play purchase flow.
4. Observing purchase updates.
5. Querying existing purchases for restore.
6. Sending purchase tokens to the verification boundary.
7. Acknowledging completed purchases as required.

The backend verification service is a separate billing boundary, not a health-data service. It accepts a Google Play purchase token and package/product context, verifies the purchase with the Google Play Developer API, and returns a minimal entitlement snapshot. It must not receive profiles, measurements, calculation results, or encrypted transfer contents.

The application should not trust a locally supplied `Premium` flag or an unverified purchase token. The app can cache the latest verified snapshot for offline use, but a new purchase or restore must be verified before Premium is granted.

### Accountless restoration

The app will not introduce application accounts in this slice. Restore uses Google Play's purchase query on the device's Google account, then sends the returned purchase token to the verification boundary. The verified purchase token is the billing identity; no personal health data is associated with it.

The Settings Premium screen exposes:

- current entitlement state;
- current plan and expiry/renewal information when available;
- Subscribe or Upgrade;
- Restore purchases;
- Manage subscription through the Google Play subscription deep link.

### Local cache and offline behavior

The app stores only the last verified entitlement snapshot locally. It includes:

- entitlement tier;
- subscription state;
- product/base-plan identifier;
- entitlement expiration time when known;
- last successful verification time.

When offline, the app may continue Premium access while the cached entitlement is active. It must not grant Premium solely because a user changes a local preference. Once the cached entitlement is known to be expired, Premium-only actions are disabled until a successful verification restores access. Local data remains readable and recordable regardless of billing connectivity.

Subscription states must distinguish at least active, canceled-but-not-expired, grace period, account hold, paused, pending, and expired. The app grants Premium during active and valid grace-period states returned by verification, does not grant Premium for pending purchases until completed, and removes Premium at expiration or revocation.

## Data and feature boundaries

### Past measurements

Past-date support is implemented as a domain/application measurement concern before the Premium gate is added. Persistence stores the measurement timestamp in UTC, as it does for current measurements. Presentation converts the selected date and time for display. Free users can record measurements for the current date only; Premium users can choose a past date subject to validation.

### Display units

Canonical persisted and formula units remain kilograms and centimeters. Free users use metric display units. Premium users can select metric or imperial display units in Settings. Unit conversion remains Presentation-owned for display and input mapping; formulas and persistence remain metric.

Changing language, date format, or theme remains available to Free users. The unit selector should be visible but marked Premium for Free users, so the limitation is discoverable and localized.

### Measurement graphics

The existing weight chart remains available to Free users in kilograms. Premium unlocks the remaining supported display-unit choices and future chart series. Chart data remains local and is not uploaded for entitlement or analytics purposes.

### Encrypted transfer

The existing encrypted `.anthropometry` transfer format remains the implementation basis. Premium access is enforced before export and import in the Application use cases. The transfer cryptography, passcode flow, validation, and file sharing boundaries remain independent of billing.

### Profiles

Free users may create one profile. Premium users may create up to ten. Both creation and import use the same Application feature policy, preventing an import path from bypassing the limit.

## Error handling and user experience

- Play Billing unavailable: show a recoverable localized message and keep the local app usable.
- Purchase canceled by the user: return to the Premium screen without changing entitlement.
- Purchase pending: show pending state and do not unlock Premium yet.
- Verification unavailable after a purchase: show that verification is pending; do not silently claim success.
- Restore finds no purchase: explain that no active Premium purchase was found for the current Google Play account.
- Subscription canceled but not expired: retain Premium until the verified expiration time.
- Subscription expired or revoked: disable new Premium actions while preserving all local data.
- Backend or Play response is malformed: fail closed for new Premium access and log only non-sensitive diagnostic information.

The app must not log purchase tokens, profile names, measurements, calculation results, passphrases, or transfer contents.

## Testing strategy

### Domain/Application tests

- Free users can access core measurements, history, results, and kilogram weight graphics.
- Free users cannot create a second profile, record a past measurement, select imperial units, use full graphics, or import/export encrypted profiles.
- Premium users can access all listed Premium features up to ten profiles.
- Profile creation and profile import enforce the same limit.
- Expired, revoked, pending, and unverified snapshots fail closed for Premium actions.
- Active, canceled-but-not-expired, and valid grace-period snapshots preserve Premium access.
- Existing local profiles and measurements remain readable after entitlement loss.

### Presentation tests

- Premium controls show a localized locked state for Free users.
- Selecting a locked control navigates to the Premium screen.
- Settings exposes subscribe, restore, and manage-subscription actions.
- Unit controls remain usable for Premium users and visibly gated for Free users.
- Billing errors do not clear local data or disrupt the core measurement flow.

### Android verification

- Use Google Play Billing test products and license testers.
- Verify purchase, restore, cancellation, renewal, pending, grace-period, account-hold, and expiration flows.
- Verify the app behaves correctly without network access after a previously verified purchase.
- Verify the Play subscription-management deep link.

## Implementation order

1. Implement and test the Application entitlement model and centralized feature policy with a local fake entitlement provider.
2. Implement past-date measurements and their persistence validation.
3. Apply feature gates to profiles, units, graphics, and encrypted transfer.
4. Add the Premium Settings UI and localized locked-state copy.
5. Add the Android Google Play Billing adapter.
6. Add the minimal verification backend and integration contract.
7. Add Play test-product verification and release configuration.

No payment package, backend, database migration, or UI gate should be added before the implementation plan is approved.
