# Profile and Measurement UX Design

## Status

Approved design, with the profile limit action changed to a disabled button.

## Context

The MVP currently asks for all measurement inputs on every measurement entry, stores profile names only, and calculates results after every entry. The next slice needs to support persistent profile settings, weight-only entries, size-based entries, clearer copy, a four-profile limit, and a clearer first-use experience without moving calculation logic into the UI.

## Goals

- Keep profile settings for height, age, and activity level in local SQLite storage.
- Let users record a new weight without entering sizes while preserving historical results.
- Keep an extended entry for weight, neck, and abdomen sizes, using the saved profile settings.
- Preserve historical measurements and calculation results exactly as recorded.
- Show a warning icon with weight-only results because they reuse the latest earlier size measurements.
- Enforce a maximum of four profiles in the application layer and reflect the limit in the UI with a disabled `Add profile` button.
- Normalize all user-facing copy and display dates as `dd/MM/yyyy`.
- Add non-functional `Settings` and `Help` toolbar items.
- Keep the design ready for future measurement graphs without coupling the domain to MAUI or SQLite.

## Non-goals

- Increasing the profile limit through payments, advertising, accounts, or synchronization.
- Implementing Settings or Help behavior.
- Recalculating historical results after a profile setting or weight-only change.
- Adding graph screens in this slice.
- Adding a network service, analytics, or a new external package.

## Recommended approach

Extend the existing `Profiles` and `Measurements` models and tables. A separate settings table or separate measurement tables would add joins and migration surface without supporting a current use case.

### Profile settings

Add height, age, and activity level to the profile editing flow. New profiles must provide valid values. Existing profiles created before this change may have incomplete settings until edited; their settings are populated from the latest existing measurement when possible.

Profile settings are the source for new measurement entries. Measurement records continue to store a snapshot of height, age, and activity so historical data remains independent if the profile is edited later.

### Measurement types

Add an explicit domain measurement type with friendly presentation names:

- `Weight only`: stores the new weight and timestamp. It copies the current profile settings into the measurement snapshot when available, stores no neck or abdomen sizes, and creates results using the latest earlier extended measurement's neck and abdomen values.
- `Weight and sizes`: stores weight, neck, and abdomen together with the profile setting snapshot. It creates new body-fat, BMR, and TDEE results using the existing formula versions.

An extended entry must contain both neck and abdomen. A partial size entry is rejected with an actionable validation message. Existing measurements are treated as extended measurements because they already contain the size fields.

The UI presents only the fields relevant to the selected method. Height, age, and activity are read from the profile and are not manually re-entered on the measurement page.

### Results and warning state

Saving either measurement type returns to the profile without opening a results page. Add weight is disabled until an earlier extended measurement exists. Weight-only saves persist new results for the new weight using the earlier neck and abdomen values; extended saves persist results from their entered sizes. Both remain available from History. The profile detail screen does not show a warning icon; the results page shows it for weight-only entries.

The results page keeps two-decimal values, friendly titles, and units only. Weight-only results additionally show a warning icon.

### Profile list behavior

- Zero profiles: show the centered empty state and its create-profile action; do not show the top `Add profile` action.
- One to three profiles: show the top `Add profile` action enabled.
- Four profiles: keep `Add profile` visible but disabled. The application use case also rejects a fifth profile, so the rule is not UI-only.

The empty-state copy will use friendly wording such as `No profile yet. Create one to get started.` All buttons, labels, picker values, titles, and error messages will avoid raw property or enum names.

### Toolbar items and dates

The profile list page will expose `Settings` and `Help` as visible top toolbar items. Their commands are deliberate no-ops until a later slice.

Dates in history and other user-facing measurement summaries will use `dd/MM/yyyy`. Persistence continues to store UTC values; formatting remains in Presentation.

## Persistence and migration

Add an explicit migration after the current schema version:

- Add nullable profile setting columns to preserve profiles that predate this feature.
- Add the measurement type column with existing rows classified as extended.
- Rebuild or otherwise migrate the Measurements table as needed so neck and abdomen can be absent for weight-only records.
- Backfill profile settings from each profile's latest existing measurement when available.

The repositories map the new fields to domain types and do not perform calculations. Calculation result rows are not altered by weight-only entries.

## Application boundaries

- `CreateProfile` enforces the maximum of four profiles and returns a specific limit error.
- A profile update use case validates and persists name plus profile settings.
- `RecordMeasurement` validates the selected measurement type and required fields, reads the profile settings through the application port, and persists the measurement.
- Calculation use cases run only for extended measurements in the presentation workflow; the existing calculation use cases and formula implementations remain domain/application concerns.
- Presentation calls use cases and translates domain/application errors into friendly messages.

No domain or application code will reference MAUI, Android, XAML, or SQLite.

## Verification plan

- Domain tests cover profile settings validation, measurement types, required/absent sizes, units, and historical snapshots.
- Application tests cover the four-profile limit, profile updates, weight-only persistence, extended persistence, and recalculation from the latest earlier extended measurement.
- Infrastructure tests cover the migration, nullable size columns, existing-row compatibility, and round-trip mappings.
- ViewModel tests cover the empty-state/top-action transitions, disabled state at four profiles, both measurement entry modes, warning state, friendly copy, and date formatting.
- Run the full Release restore, build, and test commands, followed by an Android-specific build because the MAUI UI and Android target change.

## Risks and mitigations

- Legacy profiles may not have saved settings. Keep those columns nullable during migration and require completion before an extended entry; do not invent personal values.
- Editing profile settings could otherwise change historical calculations. Store the settings snapshot on each measurement and never recalculate old results.
- A UI-only profile limit could be bypassed by another caller. Enforce the limit in `CreateProfile` as well as disabling the button.
