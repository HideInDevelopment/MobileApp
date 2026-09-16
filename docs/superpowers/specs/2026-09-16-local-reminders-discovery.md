# Local reminders discovery

**Status:** Deferred. No reminder implementation, package, permission, or
manifest change is approved by this discovery.

## Decision

Local measurement reminders do not belong in the current V2 implementation.
The reviewed V2 feedback did not establish a clear need for configurable
7-day, 14-day, or 30-day reminders, nor a preferred default behavior. The
product remains an offline-first tracker with no background notification
surface.

Revisit reminders only through a separate approved implementation slice if
future acceptance produces explicit demand. That slice must keep reminders
local, opt-in, disabled by default, cancellable, and independent of any
account or synchronization service.

## Discovery questions for a future validation round

Ask users, without collecting health measurements or identifying information:

1. Would a reminder to record a measurement be useful?
2. Which cadence is preferred: 7, 14, or 30 days?
3. Should reminders be disabled by default and enabled only from Settings?
4. Should each profile have its own reminder, or should there be one app-level
   reminder?
5. Would users trust a reminder that stays entirely on the device?
6. What should happen when a measurement is already recorded before the next
   reminder fires?

The answers should establish a clear use case before any permission or
background scheduling work begins.

## Platform and reliability constraints

### Android

- Android 13 and later requires the runtime `POST_NOTIFICATIONS` permission for
  non-exempt notifications. Newly installed apps have notifications disabled
  until the user grants it, so the permission request must follow an explicit
  user action and explain the benefit.
- Exact alarms are unsuitable for a periodic measurement reminder. On Android
  12 and later they require special Alarms & reminders access, and the system
  recommends inexact alarms whenever precise timing is not core to the app.
- Inexact alarms and periodic background work can be delayed by Doze, battery
  saving, and other scheduler constraints. The user-facing copy must never
  promise delivery at an exact time.
- Alarms are cleared when the device shuts down. A future implementation would
  need a boot rescheduling path, cancellation handling, and a safe response to
  revoked alarm access.

### MAUI and future iOS portability

- .NET MAUI documents local notifications as a shared abstraction backed by
  platform-specific implementations. Adding the feature would therefore need
  an application-facing boundary plus Android and future iOS implementations;
  notification code must not leak into Domain.
- iOS requires UserNotifications authorization before displaying alerts,
  sounds, or badges. Scheduled requests have identifiers that must be retained
  so they can be cancelled when a reminder is disabled or its cadence changes.
- The current Android-only target does not justify a cross-platform package.
  Platform APIs are sufficient for a future implementation, but a package
  should be considered only after the requirements and supported targets are
  approved.

## Privacy and product constraints

If reminders are approved later:

- store only the opt-in state, cadence, profile association, and next local
  schedule in the private app database or local preferences;
- do not put weight, body-fat, BMR, TDEE, or circumference values in a
  notification title, body, log, or analytics event;
- provide an explicit disable/cancel action and recover cleanly when system
  permission is revoked;
- use the selected language and local date/time rules for notification copy;
- do not add network access, accounts, analytics, or cloud scheduling.

## Future-slice boundary

A later approved slice may define the reminder domain contract, Settings UI,
platform permission flow, scheduling adapter, reboot recovery, cancellation,
localization, and tests. It must not silently expand into coaching,
notifications for results, population comparisons, or remote messaging.

## Sources

- [Android notification runtime permission](https://developer.android.com/develop/ui/compose/notifications/notification-permission)
- [Android alarms and reminders](https://developer.android.com/develop/background-work/services/alarms)
- [.NET MAUI local notifications](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/local-notifications?view=net-maui-10.0)
- [Apple notification authorization](https://developer.apple.com/documentation/usernotifications/asking-permission-to-use-notifications)
- [Apple local notification scheduling](https://developer.apple.com/documentation/usernotifications/scheduling-a-notification-locally-from-your-app)
