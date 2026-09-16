# Unified Measurement System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace independent display-unit choices with one persisted Metric/Imperial preference while keeping all stored and calculated values in kilograms and centimeters.

**Architecture:** Keep the measurement system in Presentation. `DisplayPreferencesService` derives the display units and owns metric/imperial conversions and legacy preference migration; ViewModels convert at the UI boundary. Domain, Application, SQLite schema, formulas, and persisted canonical values remain unchanged.

**Tech Stack:** .NET 10, .NET MAUI/XAML, CommunityToolkit.Mvvm, xUnit, existing MAUI Preferences storage.

**Spec:** `docs/superpowers/specs/2026-09-16-unified-measurement-system-design.md`

## Global Constraints

- Persist and calculate height/circumferences in centimeters and weight in kilograms.
- Metric displays meters for profile height, centimeters for circumferences, and kilograms for weight.
- Imperial displays decimal feet for profile height, inches for circumferences, and pounds for weight.
- Do not use feet-and-inches shorthand; `5.1 ft` means `5.1 × 30.48 cm`.
- Keep decimal precision through conversion and parse both dot and comma decimal input using the existing Presentation behavior.
- Do not add packages, database migrations, or Domain/Application dependencies on Presentation.

---

### Task 1: Unified preference model and conversion boundary

**Files:**
- Modify: `src/Anthropometry.App/Display/IDisplayPreferenceStore.cs`
- Modify: `src/Anthropometry.App/Display/PreferencesDisplayPreferenceStore.cs`
- Modify: `src/Anthropometry.App/Display/DisplayPreferencesService.cs`
- Modify: `tests/Anthropometry.App.Tests/Support/Fakes.cs`
- Modify: `tests/Anthropometry.App.Tests/Display/DisplayPreferencesServiceTests.cs`

**Steps:**

- [x] Add failing tests for Metric/Imperial defaults, persistence, legacy `height-unit` migration, and conversion round trips for meters, feet, centimeters, inches, kilograms, and pounds.
- [x] Run the focused display-preference tests and confirm the failures are caused by the missing unified system contract.
- [x] Add `MetricCode` and `ImperialCode`, derive `WeightUnitCode`, `HeightUnitCode`, and a circumference unit from the selected system, and add explicit height/circumference conversion helpers.
- [x] Change the preference store to persist a `measurement-system` code and map existing `cm`, `ft`, `in`, `kg`, and `lb` preferences when the new code is absent.
- [x] Run the focused tests and refactor only for clarity while preserving the canonical conversion boundary.

**Verification:** `dotnet test tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~DisplayPreferencesServiceTests`

### Task 2: Settings selector and localized unit names

**Files:**
- Modify: `src/Anthropometry.App/Features/Settings/SettingsViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Settings/SettingsPage.xaml`
- Modify: `src/Anthropometry.App/Localization/LanguageService.cs`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.resx`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.es.resx`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.de.resx`
- Modify: `tests/Anthropometry.App.Tests/Features/Settings/SettingsViewModelTests.cs`
- Modify: `tests/Anthropometry.App.Tests/Features/Settings/SettingsPageMarkupTests.cs`

**Steps:**

- [x] Update tests to require one localized Measurement system selector with Metric and Imperial options and no separate weight/height selectors.
- [x] Run the focused settings tests and confirm the expected failures.
- [x] Replace independent selector state with the unified selector and persist changes through `DisplayPreferencesService`.
- [x] Update XAML bindings and add English, Spanish, and German resource keys for the system and display-unit abbreviations.
- [x] Run settings ViewModel, markup, and localization tests.

**Verification:** `dotnet test tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~Settings`

### Task 3: Profile and measurement editor conversions

**Files:**
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileEditorViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileEditorPage.xaml`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementEditorViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementEditorPage.xaml`
- Modify: `tests/Anthropometry.App.Tests/Features/Profiles/ProfileEditorViewModelTests.cs`
- Modify: `tests/Anthropometry.App.Tests/Features/Measurements/MeasurementEditorViewModelTests.cs`
- Modify: `tests/Anthropometry.App.Tests/ResponsiveLayoutTests.cs`

**Steps:**

- [x] Add failing tests for metric `1.8 m`, imperial `5.1 ft`, metric centimeter circumferences, imperial inch circumferences, decimal weight, and comma decimal input.
- [x] Run the focused editor tests and confirm failures before production changes.
- [x] Convert profile height as meters/feet to canonical centimeters; convert measurement weight as kilograms/pounds and circumferences as centimeters/inches.
- [x] Reformat active editor values when the system changes without losing canonical meaning, and keep decimal keyboard/input support on every measurement, height, and weight field.
- [x] Run the focused editor and responsive tests.

**Verification:** `dotnet test tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~ProfileEditorViewModelTests|FullyQualifiedName~MeasurementEditorViewModelTests|FullyQualifiedName~ResponsiveLayoutTests"`

### Task 4: History, results-related displays, and weight graphic

**Files:**
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementHistoryViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Measurements/WeightGraphicViewModel.cs`
- Modify: `tests/Anthropometry.App.Tests/Features/Measurements/MeasurementHistoryViewModelTests.cs`
- Modify: `tests/Anthropometry.App.Tests/Features/Measurements/WeightGraphicViewModelTests.cs`

**Steps:**

- [x] Add failing tests for metric history height/weight, imperial history height/weight, imperial weight chart values, and canonical point retention.
- [x] Run the focused history/chart tests and confirm failures.
- [x] Use the service-derived display units for history and chart labels/values while leaving chart points and results untouched in canonical units.
- [x] Run the focused history/chart tests and verify language refreshes remain correct.

**Verification:** `dotnet test tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~MeasurementHistoryViewModelTests|FullyQualifiedName~WeightGraphicViewModelTests"`

### Task 5: Documentation, full verification, and handoff

**Files:**
- Modify: `ARCHITECTURE.md`
- Modify: `PLAN.md`
- Modify: `docs/superpowers/plans/2026-09-15-display-preferences-plan.md`

**Steps:**

- [x] Update the architecture and active plan to describe the unified Metric/Imperial preference, meter/feet profile height, centimeter/inch circumferences, and canonical kg/cm storage.
- [x] Review the complete diff for stale independent-unit references, missing localization keys, unrelated edits, and architecture violations.
- [ ] Run `dotnet restore` (host-level .NET workload check exits before project evaluation because Windows reports a pending restart; existing restored assets are valid and the Android build restored successfully).
- [x] Run `dotnet test Anthropometry.sln -f net10.0 --configuration Release`.
- [x] Run the Android Debug/Release build with the repository’s SDK/JDK overrides and confirm 0 warnings/errors.
- [x] Commit the completed implementation with a focused message.

**Verification:**

```powershell
dotnet restore
dotnet test Anthropometry.sln -f net10.0 --configuration Release
dotnet build src/Anthropometry.App/Anthropometry.App.csproj -f net10.0-android --configuration Release -p:PublishTrimmed=false -p:RunAOTCompilation=false
```
