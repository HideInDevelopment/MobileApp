# Display Preferences Implementation Plan

> Execute this plan in small TDD slices. Keep domain/application contracts metric and UTC throughout.

## Goal

Allow users to persist a date order, weight unit, and height unit, and apply those choices consistently to settings, profile/measurement forms, history, results-related measurement displays, and the weight graphic.

## Constraints

- No new package and no SQLite migration.
- Domain and Application remain independent of MAUI, Preferences, cultures, and display units.
- Persist weights as kilograms, lengths as centimeters, and timestamps as UTC.
- Keep formula calculations and calculation-result units unchanged.
- Default to `dd/MM/yyyy`, kilograms, and centimeters.

## Slice 1: Presentation preference model and persistence

1. Add canonical preference codes and immutable option records for date order, weight unit, and height unit.
2. Add a Presentation-owned preference store backed by MAUI Preferences, with one key per setting.
3. Add a singleton service that loads defaults, validates saved values, exposes current selections, persists changes, and raises one change notification.
4. Add conversion and formatting helpers for kilograms/pounds, centimeters/decimal feet for profile height, and local date formatting.
5. Add focused tests for defaults, persistence, invalid-value fallback, conversion precision, and both date orders.

## Slice 2: Settings UI

1. Inject the preference service into `SettingsViewModel` and expose localized option lists and selected values.
2. Add date, weight, and height selectors to `SettingsPage.xaml`.
3. Add English, Spanish, and German resource keys for setting labels and options.
4. Refresh selector labels and selected values when language or display preferences change.
5. Add ViewModel and markup tests for the new settings behavior.

## Slice 3: Profile and measurement input boundaries

1. Inject the preference service into profile and measurement editor view models.
2. Display existing canonical height in the selected height unit when editing a profile.
3. Parse selected-unit height and convert to centimeters before create/update commands.
4. Parse selected-unit weight and circumference values and convert to kilograms/centimeters before `RecordMeasurement`.
5. Update form placeholders/labels to show the selected unit and refresh them after a preference change.
6. Add tests proving user-entered imperial values persist as canonical metric values and existing metric values display in the selected units.

## Slice 4: History, results, and chart display

1. Inject the preference service into history and result view models.
2. Format history dates using local time and the selected order; convert displayed weight and height.
3. Update result-adjacent measurement displays if present without changing percentage or energy units.
4. Pass selected-unit formatting into the weight graphic while keeping `WeightGraphicPoint.WeightKg` canonical.
5. Convert chart Y-axis ticks, padding, and legend weights to the selected unit; format compact and full dates using the selected order.
6. Add tests for history formatting, chart conversion, chart date labels, and preference-change refreshes.

## Slice 5: Composition, lifecycle, and verification

1. Register the preference store/service in `MauiProgram` and pass it through `MauiNavigation` constructors.
2. Ensure settings survive app restart through the existing Preferences-backed store.
3. Review all remaining hard-coded `dd/MM/yyyy`, `Kg`, `Cm`, `WeightKg`, and `HeightCm` Presentation strings with `rg`.
4. Run focused App tests after each slice, then the full `net10.0` suite.
5. Build the Android Release target and review the final diff for architectural, privacy, and localization issues.
6. Update `PLAN.md` with the completed slice and verification commands.

## Expected verification

```powershell
dotnet test Anthropometry.sln -f net10.0 --configuration Release
dotnet build src/Anthropometry.App/Anthropometry.App.csproj -f net10.0-android -c Release
```

## Files expected to change

- `src/Anthropometry.App/Settings/` or equivalent Presentation preference files.
- `src/Anthropometry.App/Features/Settings/`.
- `src/Anthropometry.App/Features/Profiles/`.
- `src/Anthropometry.App/Features/Measurements/`.
- `src/Anthropometry.App/Features/Results/` if a measurement display requires it.
- `src/Anthropometry.App/Localization/` and all three resource files.
- `src/Anthropometry.App/MauiProgram.cs` and `src/Anthropometry.App/MauiNavigation.cs`.
- `tests/Anthropometry.App.Tests/`.
- `PLAN.md` after implementation verification.
