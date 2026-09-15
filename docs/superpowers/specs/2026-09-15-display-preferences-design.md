# Display Preferences Design

## Scope

Add persisted user preferences for date order, weight unit, and height unit. The preferences affect Presentation inputs and display only; domain calculations and SQLite persistence remain metric and UTC.

## Decisions

- Store preferences in the existing local MAUI Preferences mechanism through a Presentation-owned preference store.
- Use canonical codes for the three settings and validate unknown saved codes by falling back to defaults.
- Defaults are `dd/MM/yyyy`, kilograms, and centimeters.
- Store all weights as kilograms, all lengths as centimeters, and all timestamps as UTC.
- Convert profile and measurement input values to metric before invoking Application use cases.
- Convert metric values for profile/measurement display, history, and the weight chart.
- Format persisted timestamps in device local time at the Presentation boundary, using the selected date order.
- Use `1 kg = 2.2046226218 lb` and `1 in = 2.54 cm`; retain decimal precision internally and apply friendly formatting only for display.
- Notify Presentation consumers when a preference changes so already-created settings and feature view models can refresh their derived text.
- Keep calculated result units unchanged because body-fat percentage and energy results are not affected by display unit selection.

## Affected flows

1. Settings exposes date format, weight unit, and height unit selectors alongside language and theme.
2. Profile editor displays and parses height in the selected height unit, converting to centimeters for create/update commands.
3. Measurement editor displays and parses weight and circumference values in the selected units, converting to kilograms/centimeters before recording.
4. History displays local dates, weight, and height in the selected formats and units.
5. Weight graphic displays selected-unit Y-axis values, legends, and compact date labels while retaining canonical metric points internally.

## Boundaries and dependencies

The new service and its store live in `Anthropometry.App`. Domain and Application remain unaware of MAUI, Preferences, display cultures, and user-selected units. No package or database migration is required.

## Validation and testing

- Unit tests cover preference defaults, persistence, invalid-code fallback, conversions, and date formatting.
- ViewModel tests cover selected-unit input conversion, display conversion, settings selection, and preference-change refresh behavior.
- Markup tests cover the three new settings controls and unit-boundary bindings where applicable.
- Existing domain/application/infrastructure tests must remain unchanged and passing.
- The final slice requires the normal test suite plus Android Release build verification.

## Not in scope

- Changing formula inputs, formula identities, or calculation-result units.
- Storing user display preferences in SQLite.
- Locale-dependent automatic date/unit selection beyond the explicit settings.
- Editing historical measurements.
