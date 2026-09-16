# Anthropometric Tracking App V2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the app specific enough to communicate a clear product promise while remaining understandable to people without prior anthropometry knowledge, then improve the highest-value guidance, history, and trend-tracking workflows.

**Architecture:** Keep the product promise and guidance in Presentation, keep validation and measurement semantics in Domain/Application, and preserve the existing offline SQLite boundary. V2 uses progressive disclosure: the default flow stays short and plain-language, while measurement instructions, formula detail, and historical context are available on demand. No beginner/expert mode, backend, analytics SDK, or population-data service is introduced.

**Tech Stack:** .NET 10, .NET MAUI, C#, XAML, `CommunityToolkit.Mvvm`, existing SQLite infrastructure, xUnit, existing custom chart drawable, local MAUI Preferences, and the existing English/Spanish/German resource files. No new package is planned for the first V2 slices.

**Spec:** `ARCHITECTURE.md`, `PLAN.md`, and the product direction and constraints in this document.

## Global Constraints

- The primary audience is adults who want to record body measurements at home and understand personal trends over time.
- The product promise is: private, offline body-measurement tracking with understandable estimates and personal trends.
- Users do not need prior anthropometry knowledge to complete the main flow.
- Experienced users must be able to complete the main flow without reading every help topic.
- Guidance uses progressive disclosure: short inline explanation first, deeper instructions on demand.
- Guidance must describe estimates accurately and must not present the app as a medical diagnostic or professional assessment tool.
- All user-facing copy remains localizable in English, Spanish, and German.
- Domain and Application code must not reference MAUI, XAML, Android, SQLite, resources, or Preferences.
- Presentation must call Application use cases and must not access repositories directly.
- Raw measurements remain stored independently from calculated results; historical results retain formula identity and version.
- Dates remain UTC in persistence and are converted only for Presentation.
- No network service, account system, analytics SDK, synchronization, or population-average dataset is added in V2.
- Existing metric/imperial conversion behavior and the existing custom weight chart remain the baseline.
- Every implementation slice follows TDD: focused failing test, meaningful failure, minimal implementation, focused pass, full applicable verification.
- Every completed slice receives a privacy review and a focused commit.

## Product Direction

### Positioning decision

Use a narrow job-to-be-done with broad accessibility:

> “For people who want to track body measurements privately at home, this app records consistent measurements and explains personal estimates and trends without requiring specialist knowledge.”

The app is not positioned as a clinical tool, a coaching platform, a population-comparison product, or an advanced professional anthropometry suite.

### Experience principles

1. **Specific promise, inclusive workflow.** The audience and core job stay concrete; the user interface does not assume expertise.
2. **Fast path first.** The first measurement should require only the information needed for the selected measurement type.
3. **Explain at the point of uncertainty.** Activity levels, anatomical measurement locations, reused size values, and calculated metrics receive contextual help beside the relevant control.
4. **Technical detail remains available.** Formula names, versions, units, and equations stay in Help or an on-demand details surface rather than being removed.
5. **Trust before novelty.** Accurate labels, actionable validation, estimates disclaimers, local storage messaging, and consistent history take priority over advanced analytics.

### Research basis

- The mHealth engagement review identified personalization, reinforcement, communication, navigation, credibility, message presentation, and interface aesthetics as recurring engagement themes: [JMIR design-features review](https://www.jmir.org/2020/12/e21687).
- Retention remains difficult; usefulness, feedback, reminders, and in-app support are more actionable levers than simply widening the audience: [JMIR retention review](https://www.jmir.org/2022/4/e35120/).
- Health-literacy guidance recommends plain language, simple navigation, actionable content, and testing with people who have different literacy and digital-health experience: [Health Literacy Online](https://odphp.health.gov/healthliteracyonline) and [HHS user-research guidance](https://odphp.health.gov/healthliteracyonline/involve-your-audience-design-process).
- WHO recommends using concrete personas and keeping beneficiaries at the center of mHealth design: [WHO personas toolkit](https://www.who.int/publications-detail-redirect/9789241516525).

## V2 Dependency Map

```text
PLAN.md Slice 8 release verification
        │
        ├── V2 Slice 1: content inventory and guidance contract
        │       │
        │       ├── V2 Slice 2: contextual form guidance
        │       └── V2 Slice 3: result explanations and direct transition
        │
        ├── V2 Slice 4: measurement edit and delete
        │       │
        │       └── V2 Slice 5: structured history and filters
        │                       │
        │                       └── V2 Slice 6: filtered trend charts
        │
        ├── V2 Slice 7: visual hierarchy and accessibility polish
        │
        └── V2 Slice 8: user validation and release handoff
                │
                ├── V2 Slice 9: optional reminders discovery gate
                └── V2 Slice 10: Android local measurement reminders
```

Population comparisons, cloud synchronization, accounts, coaching, and advanced analytics are explicitly deferred beyond this plan.

## Success Measures

The app remains analytics-free. These measures are verified through local manual acceptance sessions using synthetic data and a short feedback form rather than remote telemetry.

- A first-time user can create a profile, choose an activity level, enter a complete measurement, and reach results without external explanation.
- A first-time user can explain what each activity-level option means after seeing the examples.
- A first-time user can locate the measurement instructions for weight, neck, abdomen/waist, and hip.
- A user can explain that body-fat, BMR, and TDEE values are estimates rather than diagnoses.
- An experienced user can complete the same flow without being forced through a long tutorial.
- A user can correct a measurement mistake without deleting the entire profile.
- A user can find a recent history entry, filter the history, and open a relevant trend chart.
- The main workflows remain usable with large text, small Android screens, keyboard open, and English, Spanish, and German selected.

## Planned File Map

The implementation should reuse existing feature folders and avoid a new cross-cutting framework.

- Modify: `src/Anthropometry.App/Features/Profiles/ProfileEditorPage.xaml`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileEditorViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Profiles/ActivityLevelOption.cs`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementEditorPage.xaml`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementEditorViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementHistoryPage.xaml`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementHistoryViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementHistoryItem.cs`
- Modify: `src/Anthropometry.App/Features/Measurements/WeightGraphicPage.xaml`
- Modify: `src/Anthropometry.App/Features/Measurements/WeightGraphicViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Results/CalculationResultPage.xaml`
- Modify: `src/Anthropometry.App/Features/Results/CalculationResultViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Help/HelpPage.xaml`
- Modify: `src/Anthropometry.App/MauiNavigation.cs`
- Modify: `src/Anthropometry.App/Resources/Styles/Styles.xaml`
- Modify: `src/Anthropometry.App/Resources/Styles/Colors.xaml`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.resx`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.es.resx`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.de.resx`
- Modify: `src/Anthropometry.Application/Abstractions/IMeasurementRepository.cs`
- Modify: `src/Anthropometry.Application/Common/ApplicationModels.cs`
- Modify: `src/Anthropometry.Infrastructure/Repositories/SqliteMeasurementRepository.cs`
- Create: focused Application use cases and Presentation models only where an existing file cannot keep one responsibility.
- Modify: corresponding files under `tests/Anthropometry.Application.Tests`, `tests/Anthropometry.Infrastructure.Tests`, and `tests/Anthropometry.App.Tests`.

---

### V2 Slice 1: Guidance content contract and audience language

**Review boundary:** All guidance required by the target audience is identified, localized, consistent with the formulas and units, and available through one Presentation-owned content contract. No production workflow changes beyond content wiring are included in this slice.

**Files:**

- Create: `src/Anthropometry.App/Features/Help/GuidanceTopic.cs`
- Create: `src/Anthropometry.App/Features/Help/GuidanceContent.cs`
- Modify: `src/Anthropometry.App/Features/Profiles/ActivityLevelOption.cs`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.resx`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.es.resx`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.de.resx`
- Modify: `src/Anthropometry.App/Features/Help/HelpPage.xaml`
- Test: `tests/Anthropometry.App.Tests/Features/Help/GuidanceContentTests.cs`
- Test: `tests/Anthropometry.App.Tests/Features/Profiles/ActivityLevelOptionTests.cs`

**Interfaces:**

- `GuidanceTopic` produces stable Presentation topics for `ActivityLevel`, `Weight`, `Neck`, `Abdomen`, `Waist`, `Hip`, `BodyFat`, `Bmr`, `Tdee`, `ReusedSizes`, and `EstimateDisclaimer`.
- `GuidanceContent.Get(LanguageService, GuidanceTopic)` produces a localized title and body without reading Domain or Application objects.
- `ActivityLevelOption` produces a display name and a short localized description while preserving its existing `ActivityLevel Value`.

- [x] **Step 1: Add failing tests for topic coverage and localized content**

  Assert that every `GuidanceTopic` returns a non-empty title and body in `en`, `es`, and `de`, and that no body contains an untranslated resource key.

- [x] **Step 2: Run the focused tests and verify the failure**

  Run:

  ```powershell
  dotnet test tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~GuidanceContentTests|FullyQualifiedName~ActivityLevelOptionTests"
  ```

  Expected: the tests fail because the new topic/content contract does not exist.

- [x] **Step 3: Add the localized resource keys and content contract**

  Add concise copy for each topic to all three `.resx` files. Keep measurement instructions actionable: location, posture, tape placement, and consistency. Keep activity descriptions concrete and non-judgmental. Keep the disclaimer explicit that estimates are population-level formulas and not medical advice.

- [x] **Step 4: Run the focused tests and verify the content**

  Run the same focused command. Expected: PASS for all locales and topics.

- [x] **Step 5: Run the existing Help and localization tests**

  Run:

  ```powershell
  dotnet test tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~Help|FullyQualifiedName~Localization"
  ```

  Expected: existing Help/localization tests remain green.

- [x] **Step 6: Commit the content contract**

  ```powershell
  git add src/Anthropometry.App/Features/Help src/Anthropometry.App/Features/Profiles/ActivityLevelOption.cs src/Anthropometry.App/Resources/Strings tests/Anthropometry.App.Tests/Features/Help tests/Anthropometry.App.Tests/Features/Profiles/ActivityLevelOptionTests.cs
  git commit -m "feat: add localized guidance content contract"
  ```

---

### V2 Slice 2: Contextual guidance in profile and measurement forms

**Review boundary:** A novice can discover help beside activity-level and measurement inputs without leaving the main task, while an experienced user can ignore it. Guidance is accessible by touch and has no impact on Domain validation.

**Files:**

- Modify: `src/Anthropometry.App/Features/Profiles/ProfileEditorViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileEditorPage.xaml`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementEditorViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementEditorPage.xaml`
- Modify: `src/Anthropometry.App/Features/Measurements/IMeasurementNavigation.cs`
- Modify: `src/Anthropometry.App/MauiNavigation.cs`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.resx`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.es.resx`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.de.resx`
- Test: `tests/Anthropometry.App.Tests/Features/Profiles/ProfileEditorViewModelTests.cs`
- Test: `tests/Anthropometry.App.Tests/Features/Measurements/MeasurementEditorViewModelTests.cs`
- Test: `tests/Anthropometry.App.Tests/Features/Profiles/ProfileEditorPageMarkupTests.cs`
- Test: `tests/Anthropometry.App.Tests/Features/Measurements/MeasurementEditorPageMarkupTests.cs`

**Interfaces:**

- Extend `IProfileNavigation` with `Task ShowGuidanceAsync(GuidanceTopic topic)`.
- Extend `IMeasurementNavigation` with `Task ShowGuidanceAsync(GuidanceTopic topic)`.
- Add `IAsyncRelayCommand<GuidanceTopic> ShowGuidanceCommand` to both editor ViewModels.
- The command delegates to navigation; it does not call `Shell.Current` directly.

- [x] **Step 1: Add failing ViewModel and markup tests**

  Test that executing the profile activity-level guidance command passes `GuidanceTopic.ActivityLevel` to navigation, that female measurement forms expose waist and hip guidance topics, and that each info button has a semantic description and a minimum 48-pixel touch target.

- [x] **Step 2: Run the focused tests and verify the failure**

  ```powershell
  dotnet test tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~ProfileEditor|FullyQualifiedName~MeasurementEditor"
  ```

  Expected: FAIL because the commands, navigation methods, and markup do not yet exist.

- [x] **Step 3: Implement navigation-backed guidance actions**

  Implement `MauiNavigation.ShowGuidanceAsync` using the existing local `DisplayAlertAsync` capability. The title/body must come from `GuidanceContent` and the current `LanguageService`. Add compact info buttons beside activity level, weight, neck, abdomen/waist, and hip. Keep the form labels and units visible even when the help action is present.

- [x] **Step 4: Run focused tests and verify the novice path**

  Run the focused command again. Expected: PASS. Manually verify that each topic opens without navigating away from the form and that changing language updates the next opened guidance dialog.

- [x] **Step 5: Run the full App test project**

  ```powershell
  dotnet test tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj --configuration Release
  ```

  Expected: all existing and new App tests pass.

- [x] **Step 6: Commit contextual guidance**

  ```powershell
  git add src/Anthropometry.App/Features/Profiles src/Anthropometry.App/Features/Measurements src/Anthropometry.App/MauiNavigation.cs src/Anthropometry.App/Resources/Strings tests/Anthropometry.App.Tests/Features/Profiles tests/Anthropometry.App.Tests/Features/Measurements
  git commit -m "feat: add contextual measurement guidance"
  ```

---

### V2 Slice 3: Result explanations and direct result transition

**Review boundary:** After a successful measurement, the user reaches the calculated result directly. Each metric has a short plain-language explanation and the result screen repeats the estimate disclaimer without hiding formula/version detail from Help.

**Files:**

- Modify: `src/Anthropometry.App/Features/Results/CalculationResultViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Results/CalculationResultPage.xaml`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementEditorViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Measurements/IMeasurementNavigation.cs`
- Modify: `src/Anthropometry.App/MauiNavigation.cs`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.resx`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.es.resx`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.de.resx`
- Test: `tests/Anthropometry.App.Tests/Features/Results/CalculationResultViewModelTests.cs`
- Test: `tests/Anthropometry.App.Tests/Features/Results/CalculationResultPageMarkupTests.cs`
- Modify: `tests/Anthropometry.App.Tests/Features/Measurements/MeasurementEditorViewModelTests.cs`

**Interfaces:**

- Extend `CalculationResultDisplayItem` to include `string Description` and preserve `Title`, `Value`, and `Unit`.
- Add `Task ShowResultsAsync(ProfileDto profile, MeasurementDto measurement)` to `IMeasurementNavigation`.
- On successful calculation, `MeasurementEditorViewModel` calls `CloseMeasurementAsync` and then `ShowResultsAsync(_profile, recorded.Value)`; it no longer opens History as the first destination.

- [x] **Step 1: Add failing tests for descriptions and navigation order**

  Assert that body-fat, BMR, and TDEE items have localized descriptions, that the results page contains the estimate disclaimer, and that the measurement navigation spy records results as the first destination after save.

- [x] **Step 2: Run the focused tests and verify the failure**

  ```powershell
  dotnet test tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~CalculationResult|FullyQualifiedName~MeasurementEditor"
  ```

  Expected: FAIL because the descriptions and direct-result navigation are not implemented.

- [x] **Step 3: Implement the result display model and navigation change**

  Map each `CalculationType` to localized plain-language copy. Keep formulas, units, and formula version in Help; do not duplicate formula logic in the ViewModel. Add an explicit `View history` action that calls the existing History navigation so direct result navigation does not hide the historical record.

- [x] **Step 4: Run focused tests and verify the complete flow**

  Expected: PASS. Manually verify extended and weight-only entries, including the reused-size warning, reach Results directly and still allow navigation back to History.

- [x] **Step 5: Run Domain, Application, Infrastructure, and App tests**

  ```powershell
  dotnet test Anthropometry.sln -f net10.0 --configuration Release --no-restore -m:1
  ```

  Expected: all existing tests and new result tests pass.

- [x] **Step 6: Commit the result-flow slice**

  ```powershell
  git add src/Anthropometry.App/Features/Results src/Anthropometry.App/Features/Measurements src/Anthropometry.App/MauiNavigation.cs src/Anthropometry.App/Resources/Strings tests/Anthropometry.App.Tests/Features/Results tests/Anthropometry.App.Tests/Features/Measurements
  git commit -m "feat: explain estimates and streamline results"
  ```

---

### V2 Slice 4: Edit and delete individual measurements

**Review boundary:** Users can correct or remove a measurement while preserving profile data and the remaining history. Updating raw inputs replaces the calculation results for that measurement using the existing formula identities and versions; deleting removes the measurement and its results transactionally.

**Files:**

- Modify: `src/Anthropometry.Domain/Measurements/Measurement.cs`
- Modify: `src/Anthropometry.Application/Abstractions/IMeasurementRepository.cs`
- Create: `src/Anthropometry.Application/Measurements/UpdateMeasurement.cs`
- Create: `src/Anthropometry.Application/Measurements/DeleteMeasurement.cs`
- Modify: `src/Anthropometry.Infrastructure/Repositories/SqliteMeasurementRepository.cs`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementEditorViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementHistoryViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementHistoryPage.xaml`
- Modify: `src/Anthropometry.App/MauiNavigation.cs`
- Modify: `src/Anthropometry.App/MauiProgram.cs`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.resx`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.es.resx`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.de.resx`
- Test: `tests/Anthropometry.Domain.Tests/Measurements/MeasurementTests.cs`
- Test: `tests/Anthropometry.Application.Tests/Measurements/UpdateMeasurementTests.cs`
- Test: `tests/Anthropometry.Application.Tests/Measurements/DeleteMeasurementTests.cs`
- Test: `tests/Anthropometry.Infrastructure.Tests/Repositories/SqliteMeasurementRepositoryTests.cs`
- Test: `tests/Anthropometry.App.Tests/Features/Measurements/MeasurementHistoryViewModelTests.cs`

**Interfaces:**

- Extend `IMeasurementRepository` with `Task UpdateAsync(Measurement measurement, CancellationToken cancellationToken)` and `Task DeleteWithResultsAsync(MeasurementId id, CancellationToken cancellationToken)`.
- `UpdateMeasurement` accepts an existing `MeasurementId`, profile ID, measurement type, canonical metric values, and UTC date; it rehydrates the same ID after Domain validation.
- `DeleteMeasurement` verifies ownership through the repository and returns `ApplicationErrors.MeasurementNotFound` when appropriate.
- `MeasurementHistoryViewModel` exposes `EditCommand` and `DeleteCommand` with a confirmation callback supplied by `IMeasurementNavigation`.

- [x] **Step 1: Add failing Domain tests for same-ID rehydration and invalid edits**

  Cover valid edits that preserve `MeasurementId`, invalid size combinations, female hip requirements, and UTC date validation.

- [x] **Step 2: Add failing Application and Infrastructure tests**

  Cover update persistence, delete-with-results transaction behavior, missing measurement errors, and rollback when the SQLite delete fails.

- [x] **Step 3: Run focused tests and verify meaningful failures**

  ```powershell
  dotnet test tests/Anthropometry.Domain.Tests/Anthropometry.Domain.Tests.csproj --configuration Release --filter "FullyQualifiedName~MeasurementTests"
  dotnet test tests/Anthropometry.Application.Tests/Anthropometry.Application.Tests.csproj --configuration Release --filter "FullyQualifiedName~UpdateMeasurement|FullyQualifiedName~DeleteMeasurement"
  dotnet test tests/Anthropometry.Infrastructure.Tests/Anthropometry.Infrastructure.Tests.csproj --configuration Release --filter "FullyQualifiedName~SqliteMeasurementRepository"
  ```

  Expected: FAIL because the update/delete ports and use cases do not exist.

- [x] **Step 4: Implement the minimum Domain, Application, and SQLite behavior**

  Keep calculation replacement explicit: delete existing results for an edited measurement, persist the edited measurement, then run the existing calculation use cases and add new versioned results. Use one SQLite transaction for measurement/result deletion. Do not add cascade behavior that changes profile deletion semantics.

- [x] **Step 5: Add edit/delete actions to History**

  Reuse the existing measurement editor with an optional measurement argument. Add a red/destructive delete action only to the confirmation surface and use localized, specific copy. After edit or delete, reload History and preserve the empty/loading/error states.

- [x] **Step 6: Run all tests and Android build**

  ```powershell
  dotnet test Anthropometry.sln -f net10.0 --configuration Release --no-restore -m:1
  dotnet build src/Anthropometry.App/Anthropometry.App.csproj -f net10.0-android -c Release -m:1
  ```

  Expected: all tests pass and the Android build has 0 warnings and 0 errors.

- [x] **Step 7: Commit measurement correction support**

  ```powershell
  git add src/Anthropometry.Domain/Measurements src/Anthropometry.Application src/Anthropometry.Infrastructure/Repositories/SqliteMeasurementRepository.cs src/Anthropometry.App/Features/Measurements src/Anthropometry.App/MauiNavigation.cs src/Anthropometry.App/MauiProgram.cs src/Anthropometry.App/Resources/Strings tests
  git commit -m "feat: allow measurement correction and deletion"
  ```

---

### V2 Slice 5: Structured history and local filters

**Review boundary:** History is easy to scan as it grows and can be filtered by date range and measurement type without changing stored data or formula behavior.

**Files:**

- Create: `src/Anthropometry.Application/Measurements/MeasurementHistoryQuery.cs`
- Modify: `src/Anthropometry.Application/Measurements/GetMeasurementHistory.cs`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementHistoryViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementHistoryItem.cs`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementHistoryPage.xaml`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.resx`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.es.resx`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.de.resx`
- Test: `tests/Anthropometry.Application.Tests/Measurements/GetMeasurementHistoryTests.cs`
- Modify: `tests/Anthropometry.App.Tests/Features/Measurements/MeasurementHistoryViewModelTests.cs`
- Create: `tests/Anthropometry.App.Tests/Features/Measurements/MeasurementHistoryFilterTests.cs`

**Interfaces:**

- `MeasurementHistoryQuery` contains `ProfileId ProfileId`, nullable `DateTimeOffset FromUtc`, nullable `DateTimeOffset ToUtc`, and nullable `MeasurementType Type`.
- `GetMeasurementHistory.ExecuteAsync(MeasurementHistoryQuery query, CancellationToken cancellationToken)` returns the same `MeasurementDto` list as the existing overload.
- Filtering is performed in Application after repository retrieval for this slice; it must compare UTC timestamps and return newest first. No SQL query API is added until measured history size requires it.
- `MeasurementHistoryViewModel` exposes `FromDate`, `ToDate`, `SelectedType`, `ClearFiltersCommand`, and reloads when filters change.

- [x] **Step 1: Add failing Application tests for date and type filtering**

  Cover inclusive UTC date boundaries, type filtering, no-filter equivalence, reversed-date validation, and empty filtered results.

- [x] **Step 2: Add failing ViewModel tests for filter state**

  Cover filter labels, clearing filters, recoverable invalid-range messaging, and preservation of edit/delete actions on filtered rows.

- [x] **Step 3: Implement query and Presentation filter controls**

  Use platform date pickers and a compact measurement-type selector. Keep the default screen clear by placing filters behind a single `Filter` action. Add a result count or an explicit “No matching measurements” message without implying data was deleted.

- [x] **Step 4: Replace stacked history content with a consistent card layout**

  Keep date, weight, height, measurement type, warning state, view-results, edit, and delete actions aligned. Use dynamic resources and existing theme colors; do not add a table package.

- [x] **Step 5: Run all tests and check responsive states**

  ```powershell
  dotnet test Anthropometry.sln -f net10.0 --configuration Release --no-restore -m:1
  dotnet build src/Anthropometry.App/Anthropometry.App.csproj -f net10.0-android -c Release -m:1
  ```

  Manually verify empty, no-match, loading, error, one-row, and large-history states on a small Android screen with the keyboard closed.

- [x] **Step 6: Commit structured history**

  ```powershell
  git add src/Anthropometry.Application/Measurements src/Anthropometry.App/Features/Measurements src/Anthropometry.App/Resources/Strings tests
  git commit -m "feat: add filterable measurement history"
  ```

---

### V2 Slice 6: Filtered trend charts and additional metrics

**Review boundary:** Users can select a date range for charts and can view trends for weight, body-fat percentage, BMR, or TDEE when persisted results exist. The chart remains local, readable, and free of a new chart dependency.

**Files:**

- Create: `src/Anthropometry.Application/Calculations/GetMetricHistory.cs`
- Create: `src/Anthropometry.Application/Common/MetricHistoryDto.cs`
- Modify: `src/Anthropometry.App/Features/Measurements/WeightGraphicViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Measurements/WeightGraphicPage.xaml`
- Modify: `src/Anthropometry.App/Features/Measurements/WeightGraphicDrawable.cs`
- Modify: `src/Anthropometry.App/MauiNavigation.cs`
- Modify: `src/Anthropometry.App/MauiProgram.cs`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.resx`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.es.resx`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.de.resx`
- Test: `tests/Anthropometry.Application.Tests/Calculations/GetMetricHistoryTests.cs`
- Modify: `tests/Anthropometry.App.Tests/Features/Measurements/WeightGraphicViewModelTests.cs`

**Interfaces:**

- `MetricHistoryQuery` contains `ProfileId`, `CalculationType`, `FromUtc`, and `ToUtc`.
- `MetricHistoryDto` contains `MeasurementId`, `MeasuredAtUtc`, `CalculationType`, `Value`, and `Unit`.
- `GetMetricHistory.ExecuteAsync(MetricHistoryQuery query, CancellationToken cancellationToken)` returns results ordered oldest to newest for chart mapping and excludes measurements without the requested persisted result.
- Chart state exposes `SelectedMetric`, `FromDate`, `ToDate`, `Points`, `IsEmpty`, and a localized legend.

- [x] **Step 1: Add failing Application tests for metric history**

  Cover weight history from measurements, calculated metric history from stored results, date filtering, chronological ordering, and missing-result exclusion.

- [x] **Step 2: Add failing chart ViewModel tests**

  Cover metric selection, date-range reload, empty state, point mapping, localized legend, and selected-unit formatting.

- [x] **Step 3: Implement the metric history use case**

  Compose existing measurement and calculation-result ports in Application. Keep the join and date filtering out of the chart page. Do not recalculate historical results merely to draw a chart.

- [x] **Step 4: Extend the existing custom chart**

  Add a compact metric selector and date-range controls. Reuse the current horizontal scrolling, point selection, axis padding, theme colors, and no-data/error states. Rename labels from `Weight graphic` to a localized metric-neutral chart title only after the UI tests cover the new copy.

- [x] **Step 5: Run full verification**

  ```powershell
  dotnet test Anthropometry.sln -f net10.0 --configuration Release --no-restore -m:1
  dotnet build src/Anthropometry.App/Anthropometry.App.csproj -f net10.0-android -c Release -m:1
  ```

  Expected: all tests pass and the Android build has 0 warnings and 0 errors.

- [x] **Step 6: Commit trend charts**

  ```powershell
  git add src/Anthropometry.Application/Calculations src/Anthropometry.Application/Common src/Anthropometry.App/Features/Measurements src/Anthropometry.App/MauiNavigation.cs src/Anthropometry.App/MauiProgram.cs src/Anthropometry.App/Resources/Strings tests
  git commit -m "feat: add filtered metric trend charts"
  ```

---

### V2 Slice 7: Visual hierarchy, profile cards, and accessibility polish

**Review boundary:** The app has a coherent visual system that distinguishes primary, secondary, and destructive actions without changing Domain or Application behavior.

**Files:**

- Modify: `src/Anthropometry.App/Resources/Styles/Colors.xaml`
- Modify: `src/Anthropometry.App/Resources/Styles/Styles.xaml`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileRowView.xaml`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileListPage.xaml`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileDetailPage.xaml`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementHistoryPage.xaml`
- Modify: `src/Anthropometry.App/Features/Results/CalculationResultPage.xaml`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.resx`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.es.resx`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.de.resx`
- Test: `tests/Anthropometry.App.Tests/Resources/LightThemeMarkupTests.cs`
- Test: `tests/Anthropometry.App.Tests/Resources/DarkThemeMarkupTests.cs`
- Create: `tests/Anthropometry.App.Tests/Features/Profiles/ProfileCardMarkupTests.cs`
- Modify: `tests/Anthropometry.App.Tests/ResponsiveLayoutTests.cs`

- [x] **Step 1: Add failing markup tests for semantic styles and actions**

  Assert that profile rows expose name as the primary label, secondary details as secondary content, primary actions have the shared primary style, and delete actions use the shared destructive style with a confirmation path.

- [x] **Step 2: Run focused markup tests and verify the failure**

  ```powershell
  dotnet test tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~Resources|FullyQualifiedName~ProfileCard|FullyQualifiedName~ResponsiveLayout"
  ```

- [x] **Step 3: Implement the smallest shared visual system**

  Add only colors, spacing, button, border, card, and text resources that are used by at least two screens. Use red only for destructive actions and error text; preserve high contrast in Light and Dark themes. Keep profile cards consistent rather than alternating arbitrary row colors.

- [x] **Step 4: Verify accessibility and responsive behavior**

  Check 48-pixel touch targets, semantic descriptions for icon-only actions, larger text without clipping, keyboard-open form layouts, and small/large Android widths.

- [x] **Step 5: Run App tests and Android build**

  ```powershell
  dotnet test tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj --configuration Release
  dotnet build src/Anthropometry.App/Anthropometry.App.csproj -f net10.0-android -c Release -m:1
  ```

- [x] **Step 6: Commit the visual system**

  ```powershell
  git add src/Anthropometry.App/Resources/Styles src/Anthropometry.App/Features/Profiles src/Anthropometry.App/Features/Measurements/MeasurementHistoryPage.xaml src/Anthropometry.App/Features/Results/CalculationResultPage.xaml src/Anthropometry.App/Resources/Strings tests/Anthropometry.App.Tests
  git commit -m "feat: strengthen app visual hierarchy"
  ```

---

### V2 Slice 8: User validation and release handoff

**Review boundary:** The focused-but-inclusive product direction is tested with both knowledgeable and non-knowledgeable users, the acceptance results are documented, and the release checklist covers privacy and supported responsive states.

**Status:** The user has confirmed that real validation is complete. The
acceptance workbooks remain the reproducible protocol, but no personal
measurements or identifying session details are stored in the repository.

**Files:**

- Create: `docs/testing/v2-guidance-acceptance.md`
- Create: `docs/testing/v2-history-chart-acceptance.md`
- Modify: `README.md` with the product promise and estimate disclaimer
- Modify: `PLANV2.MD` with completed slice status and verification results

- [x] **Step 1: Prepare synthetic test data**

  Use the existing deterministic sample-history action or a disposable local database. Do not collect or commit real health measurements, screenshots containing personal data, or device database files.

- [x] **Step 2: Run novice and experienced-user acceptance sessions**

  Recruit at least one person who understands anthropometric measurements and one person who does not. Ask both to complete the same tasks without coaching: create profile, choose activity level, find measurement help, record a measurement, interpret results, open history, edit an entry, delete an entry, and open a filtered chart.

- [x] **Step 3: Record task outcomes**

  Record completion, wrong or uncertain inputs, time to first result, help topics opened, result comprehension, and whether the user could return to history. Record observations without personal measurements or identifying information.

- [x] **Step 4: Fix only blockers found in acceptance**

  Convert each blocker into a focused failing App test before changing production code. Do not add new feature scope during this validation slice.

- [x] **Step 5: Run final verification**

  ```powershell
  dotnet restore
  dotnet build --configuration Release
  dotnet test --configuration Release
  dotnet build src/Anthropometry.App/Anthropometry.App.csproj -f net10.0-android -c Release
  ```

  Run the Android acceptance checklist on a supported emulator or device, including offline startup, database reload, deletion confirmation, large text, Light/Dark themes, Metric/Imperial units, and English/Spanish/German copy.

- [x] **Step 6: Review privacy, architecture, dependencies, and diff scope**

  Confirm there are no network calls, analytics packages, unnecessary permissions, credentials, raw measurement logs, personal sample databases, direct SQLite calls from Presentation, or formula changes outside an explicitly versioned Domain change. V2 must leave `ARCHITECTURE.md` unchanged; a new public boundary requires a separate approved slice before implementation.

- [x] **Step 7: Commit the V2 handoff documentation**

  ```powershell
  git add docs/testing README.md PLANV2.MD
  git commit -m "docs: record anthropometry app v2 acceptance"
  ```

---

### V2 Slice 9: Optional reminders discovery gate

**Review boundary:** The product decision and technical risk for optional local reminders are documented. The approved Android implementation is delivered separately in Slice 10; this slice contains the discovery decision only.

**Decision:** Approve an Android-only implementation. The user selected a
daily reminder at a chosen hour and an inactivity reminder after 7 or 14 days
without opening the app. Both use the same hour, are app-level, local, opt-in,
and disabled by default. iOS remains deferred. See
`docs/superpowers/specs/2026-09-16-local-reminders-discovery.md`.

**Files:**

- Create: `docs/superpowers/specs/2026-09-16-local-reminders-discovery.md`
- Modify: `PLANV2.MD` with the decision and explicit scope

- [x] **Step 1: Measure demand during V2 acceptance**

  Ask users whether they want configurable 7-, 14-, or 30-day reminders, whether reminders should be enabled by default, and whether they trust local-only scheduling.

- [x] **Step 2: Document platform and privacy constraints**

  Identify Android notification permission behavior, MAUI/iOS portability implications, scheduling reliability, cancellation behavior, and whether a package is necessary. Do not add a package during discovery.

- [x] **Step 3: Decide whether reminders belong in a separate implementation plan**

  Continue only if the acceptance evidence shows a clear user need and the implementation can remain local, opt-in, cancellable, and privacy-compatible. The approved path is recorded in Slice 10; no implementation belongs in this discovery slice.

The approved implementation is tracked in V2 Slice 10.

---

### V2 Slice 10: Android local measurement reminders

**Review boundary:** Android users can opt into a daily reminder at a selected
hour and/or a local inactivity reminder after 7 or 14 days without opening the
app. Scheduling remains outside Domain and Application, no iOS code is added,
and no third-party package is introduced.

**Files:**

- Modify: `src/Anthropometry.App/Features/Settings/SettingsPage.xaml`
- Modify: `src/Anthropometry.App/Features/Settings/SettingsPage.xaml.cs`
- Modify: `src/Anthropometry.App/Features/Settings/SettingsViewModel.cs`
- Create: `src/Anthropometry.App/Features/Settings/ReminderSettings.cs`
- Create: `src/Anthropometry.App/Features/Settings/ReminderScheduleCalculator.cs`
- Create: `src/Anthropometry.App/Features/Settings/ReminderNotificationCopy.cs`
- Create: `src/Anthropometry.App/Features/Settings/PreferencesReminderSettingsStore.cs`
- Create: Android permission, scheduler, alarm receiver, and boot receiver files
- Modify: Android manifest, `MainActivity`, `MauiProgram`, `App`, and localized resources
- Create: focused reminder coordinator, schedule, ViewModel, and markup tests

- [x] **Step 1: Add failing coordinator, schedule, and markup tests**
- [x] **Step 2: Implement local Settings persistence and reminder coordination**
- [x] **Step 3: Add Android notification permission and AlarmManager scheduling**
- [x] **Step 4: Add app activation, boot recovery, cancellation, and localized copy**
- [x] **Step 5: Run full tests and Android Release verification**
- [x] **Step 6: Commit the Android reminder slice**

## Deferred Scope

The following are intentionally excluded from V2:

- Population averages or “compare with average” views.
- Cloud accounts, synchronization, remote backups, or social sharing.
- Medical diagnosis, treatment advice, or clinical interpretation.
- Automatic camera/body scanning or sensor integrations.
- Coaching, messaging, or community features.
- A separate expert mode or a long mandatory onboarding tutorial.
- A third-party charting library before the existing custom chart is shown to be insufficient.
- iOS notifications and cross-platform reminder support.

## Plan Self-Review Checklist

- [ ] The plan states a concrete audience and job-to-be-done.
- [ ] The main workflow remains usable for non-knowledgeable users.
- [ ] Expert users are not forced through a long tutorial or unnecessary modal flow.
- [ ] Guidance is localized and tied to current formulas, units, and measurement semantics.
- [ ] The estimate disclaimer appears in the relevant user journey and Help.
- [ ] Edit/delete behavior preserves historical data correctness and uses transactions.
- [ ] History filters use UTC boundaries and do not alter stored records.
- [ ] Charts reuse persisted results instead of recalculating historical values.
- [ ] Visual changes do not introduce a new dependency or architecture violation.
- [ ] No acceptance step stores or commits real personal health data.
- [ ] Every implementation slice has focused tests, a full applicable test command, and a reviewable commit.
- [ ] Population comparisons remain explicitly out of the core V2 scope; Android measurement reminders are limited to the approved Slice 10 behavior.
