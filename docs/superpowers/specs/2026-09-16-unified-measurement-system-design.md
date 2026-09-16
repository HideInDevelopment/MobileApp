# Unified Measurement System Design

## Goal

Replace the separate height-unit preference with one persisted measurement-system preference that presents metric or imperial values consistently while preserving canonical kilogram/centimeter data for persistence and calculations.

## Decisions

- The supported systems are `Metric` and `Imperial`.
- Metric display units are meters for profile height, centimeters for neck/waist/abdomen/hip circumferences, and kilograms for weight.
- Imperial display units are decimal feet for profile height, inches for circumferences, and pounds for weight.
- Imperial height is decimal feet, not feet-and-inches shorthand. For example, `5.1 ft` is converted to `155.448 cm`.
- All displayed numeric inputs accept decimal values using the existing localized decimal parsing behavior.
- Persistence and formula/application boundaries remain metric: height and circumferences in centimeters, weight in kilograms, and timestamps in UTC.

## Architecture

The measurement-system preference belongs to Presentation. `DisplayPreferencesService` owns the supported system codes, persistence, migration, and conversions. Profile and measurement ViewModels convert user input at their boundary and format canonical values for display; Domain and Application contracts remain unchanged.

The existing stored height-unit preference is migrated without data loss: `cm` maps to Metric, while `ft` and the older `in` value map to Imperial. New installations persist only the unified measurement-system code.

## User-visible surfaces

- Settings replaces the height-unit selector with a measurement-system selector.
- Profile editing shows height in meters or decimal feet.
- Measurement editing shows weight in kilograms or pounds and circumferences in centimeters or inches.
- History, results-related measurement summaries, and the weight graphic format values using the selected system where applicable.
- English, Spanish, and German labels identify the system and each displayed unit.

## Validation and precision

Validation remains against canonical metric ranges after conversion. Decimal values are retained through conversion and persistence; display formatting may use the existing compact decimal format. Formula calculations continue to use full canonical precision and are not changed by this presentation feature.

## Testing

Tests cover preference defaults, persistence and legacy migration, metric/imperial conversion round trips, decimal profile and circumference input, localized unit labels, history/chart formatting, and unchanged canonical values passed to Application use cases.
