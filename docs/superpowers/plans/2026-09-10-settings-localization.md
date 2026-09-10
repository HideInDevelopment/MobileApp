# Settings and Localization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add icon-only Settings and Help toolbar actions plus a persisted English, Spanish, and German language setting.

**Architecture:** Keep language selection in the MAUI Presentation layer. A testable language service reads embedded `.resx` resources and persists a language code through a small preference-store port; MAUI supplies the `Preferences` adapter and updates application resources for live bindings.

**Tech Stack:** .NET 10, .NET MAUI, XAML, `CommunityToolkit.Mvvm`, embedded `.resx` resources, MAUI `Preferences`, SVG toolbar assets.

**Spec:** `docs/superpowers/specs/2026-09-10-settings-localization-design.md`

## Global Constraints

- English is the default when no valid preference exists.
- Selecting a language applies it immediately and writes the language code to local MAUI `Preferences`.
- App startup reads the saved language before the first feature page is created.
- Domain and Application remain independent of MAUI and localization.
- No new package is added.
- Existing user-facing text is moved to English, Spanish, and German resources.

---

### Task 1: Testable language service and Settings ViewModel

**Files:**
- Create: `src/Anthropometry.App/Localization/LanguageService.cs`
- Create: `src/Anthropometry.App/Localization/ILanguagePreferenceStore.cs`
- Create: `src/Anthropometry.App/Features/Settings/SettingsViewModel.cs`
- Create: `tests/Anthropometry.App.Tests/Localization/LanguageServiceTests.cs`
- Create: `tests/Anthropometry.App.Tests/Features/Settings/SettingsViewModelTests.cs`

**Interfaces:**
- `ILanguagePreferenceStore` exposes `string? GetLanguageCode()` and `void SetLanguageCode(string code)`.
- `LanguageService` exposes `IReadOnlyList<LanguageOption> SupportedLanguages`, `string CurrentLanguageCode`, `string Get(string key)`, `void Initialize()`, and `void SetLanguage(string code)`.
- `SettingsViewModel` exposes `IReadOnlyList<LanguageOption> Languages` and a `LanguageOption? SelectedLanguage` setter that applies and persists the selection.

- [x] **Step 1: Write failing tests**

Assert that a missing preference initializes to English, a saved `de` preference initializes to German, `SetLanguage("es")` persists and changes lookup culture, unsupported codes are rejected, and Settings exposes exactly three options with the saved option selected.

- [x] **Step 2: Run focused tests and confirm the expected missing-type failure**

```powershell
dotnet test tests/Anthropometry.App.Tests --configuration Release --filter "FullyQualifiedName~LanguageServiceTests|FullyQualifiedName~SettingsViewModelTests"
```

- [x] **Step 3: Implement the minimum service and ViewModel**

Use `ResourceManager` with base name `Anthropometry.App.Resources.Strings.AppResources`, `CultureInfo`, and the injected preference store. Keep settings state independent of MAUI so App tests can use an in-memory store.

- [x] **Step 4: Run focused tests and confirm they pass**

```powershell
dotnet test tests/Anthropometry.App.Tests --configuration Release --filter "FullyQualifiedName~LanguageServiceTests|FullyQualifiedName~SettingsViewModelTests"
```

### Task 2: Resource files, Settings navigation, and toolbar icons

**Files:**
- Create: `src/Anthropometry.App/Resources/Strings/AppResources.resx`
- Create: `src/Anthropometry.App/Resources/Strings/AppResources.es.resx`
- Create: `src/Anthropometry.App/Resources/Strings/AppResources.de.resx`
- Create: `src/Anthropometry.App/Localization/PreferencesLanguagePreferenceStore.cs`
- Create: `src/Anthropometry.App/Features/Settings/SettingsPage.xaml`
- Create: `src/Anthropometry.App/Features/Settings/SettingsPage.xaml.cs`
- Create: `src/Anthropometry.App/Resources/Images/settings.svg`
- Create: `src/Anthropometry.App/Resources/Images/help.svg`
- Modify: `src/Anthropometry.App/MauiProgram.cs`
- Modify: `src/Anthropometry.App/App.xaml.cs`
- Modify: `src/Anthropometry.App/Features/Profiles/IProfileNavigation.cs`
- Modify: `src/Anthropometry.App/MauiNavigation.cs`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileListPage.xaml`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileListViewModel.cs`

**Interfaces:**
- The MAUI adapter implements `ILanguagePreferenceStore` with `Preferences.Default`.
- `IProfileNavigation.ShowSettingsAsync()` opens `SettingsPage` through the existing navigation adapter.
- Toolbar items bind icon files and retain localized accessibility names.

- [x] **Step 1: Add failing Settings navigation test**

Verify that executing `SettingsCommand` calls `ShowSettingsAsync` on the navigation spy.

- [x] **Step 2: Run the focused test and confirm it fails**

```powershell
dotnet test tests/Anthropometry.App.Tests --configuration Release --filter "FullyQualifiedName~ProfileListViewModelTests.Settings"
```

- [x] **Step 3: Add resources, icons, adapter, page, and navigation wiring**

Apply the saved culture during `App` construction, populate application resource keys for `DynamicResource`, register the service and adapter, and replace toolbar text with `settings.svg` and `help.svg`.

- [x] **Step 4: Run focused tests and the Android build**

```powershell
dotnet test tests/Anthropometry.App.Tests --configuration Release
dotnet build src/Anthropometry.App/Anthropometry.App.csproj -f net10.0-android -c Release
```

### Task 3: Localize existing screens and computed display text

**Files:**
- Modify: `src/Anthropometry.App/App.xaml`
- Modify: `src/Anthropometry.App/AppShell.xaml`
- Modify: `src/Anthropometry.App/AppShell.xaml.cs`
- Modify: `src/Anthropometry.App/App.xaml.cs`
- Modify: `src/Anthropometry.App/MauiNavigation.cs`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileListPage.xaml`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileListViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileDetailPage.xaml`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileDetailViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileEditorPage.xaml`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileEditorViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Profiles/ActivityLevelOption.cs`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementEditorPage.xaml`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementEditorViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementHistoryPage.xaml`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementHistoryItem.cs`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementHistoryViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Results/CalculationResultPage.xaml`
- Modify: `src/Anthropometry.App/Features/Results/CalculationResultViewModel.cs`

**Interfaces:**
- XAML uses `DynamicResource` keys for static copy.
- Feature ViewModels consume `LanguageService` for error messages and derived labels; newly created feature state uses the currently selected language.

- [x] **Step 1: Write failing localization assertions for derived labels**

Assert that history labels, activity levels, result titles, and measurement editor titles change when the service language changes.

- [x] **Step 2: Run the focused tests and confirm the assertions fail**

```powershell
dotnet test tests/Anthropometry.App.Tests --configuration Release --filter "FullyQualifiedName~MeasurementHistory|FullyQualifiedName~CalculationResult|FullyQualifiedName~ProfileEditor|FullyQualifiedName~MeasurementEditor"
```

- [x] **Step 3: Replace hardcoded visible copy and refresh derived state**

Use stable resource keys, preserve the existing validation and calculation behavior, and keep warning semantics unchanged.

- [x] **Step 4: Run all tests and the Android build**

```powershell
dotnet test --configuration Release
dotnet build src/Anthropometry.App/Anthropometry.App.csproj -f net10.0-android -c Release
```

### Task 4: Documentation and final verification

**Files:**
- Modify: `ARCHITECTURE.md`
- Modify: `PLAN.md`

- [x] **Step 1: Document the Presentation localization boundary and persisted preference**

Record that translation resources and the language preference remain in the MAUI layer and that no database migration is needed.

- [x] **Step 2: Review the diff for unrelated changes and privacy issues**

Confirm that no personal data, credentials, network calls, or new package references were added.

- [x] **Step 3: Run final verification**

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
dotnet build src/Anthropometry.App/Anthropometry.App.csproj -f net10.0-android -c Release
```

The solution-level Release build was also attempted. On the current machine it reaches the Android linker and fails in the .NET task host with `MSB4216/MSB4027`; the `net10.0` solution test suite and Android builds with trimming/AOT disabled pass.

- [x] **Step 4: Commit the completed slice**

```powershell
git add ARCHITECTURE.md PLAN.md docs/superpowers/specs/2026-09-10-settings-localization-design.md docs/superpowers/plans/2026-09-10-settings-localization.md src/Anthropometry.App tests/Anthropometry.App.Tests
git commit -m "feat: add persisted language settings"
```
