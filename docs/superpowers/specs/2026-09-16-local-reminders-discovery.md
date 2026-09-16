# Local reminders discovery

**Status:** Approved for an Android-only implementation slice. iOS remains out
of scope for now.

## Decision

The user-approved reminder behavior is:

- an optional daily notification at a user-selected hour;
- an optional inactivity notification after 7 or 14 days without opening the
  app;
- one app-level schedule, not a separate reminder per profile;
- the same selected hour for both reminder types;
- local Android scheduling only, with both reminders disabled by default.

The implementation must remain local, opt-in, cancellable, and independent of
any account or synchronization service. iOS notification behavior is deferred
until a separate platform decision.

## Android platform and reliability constraints

### Android

- Android 13 and later requires the runtime `POST_NOTIFICATIONS` permission for
  non-exempt notifications. Newly installed apps have notifications disabled
  until the user grants it, so the permission request must follow an explicit
  user action and explain the benefit.
- Exact alarms are intentionally not used. On Android 12 and later they
  require special Alarms & reminders access, while these reminders can use an
  inexact target time and avoid that extra special permission.
- Inexact alarms and periodic background work can be delayed by Doze, battery
  saving, and other scheduler constraints. The user-facing copy must never
  promise delivery at an exact time.
- Alarms are cleared when the device shuts down, so the Android implementation
  must reschedule them after boot and after every app activation.
- The Android implementation uses platform APIs and does not add a package.

## Privacy and product constraints

For the approved Android implementation:

- store only the opt-in state, selected hour, inactivity interval, and last app
  open time in local Preferences;
- do not put weight, body-fat, BMR, TDEE, or circumference values in a
  notification title, body, log, or analytics event;
- provide an explicit disable/cancel action and recover cleanly when system
  permission is revoked;
- use the selected language and local date/time rules for notification copy;
- do not add network access, accounts, analytics, or cloud scheduling.

## Implementation boundary

The Android implementation adds Settings controls, notification permission
handling, local alarm scheduling, boot recovery, cancellation, localization,
and tests. It must not expand into coaching, notifications for results,
population comparisons, or remote messaging.

## Sources

- [Android notification runtime permission](https://developer.android.com/develop/ui/compose/notifications/notification-permission)
- [Android alarms and reminders](https://developer.android.com/develop/background-work/services/alarms)
- [.NET MAUI local notifications](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/local-notifications?view=net-maui-10.0)
