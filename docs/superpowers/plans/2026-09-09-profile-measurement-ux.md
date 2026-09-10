# Profile and Measurement UX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add persistent profile settings, weight-only and size-based measurement flows, a four-profile limit, normalized UI copy, date formatting, and placeholder Settings/Help actions while preserving historical results and supporting recalculated weight-only history entries.

**Architecture:** Extend the existing Domain and Application models, keep profile settings and measurement snapshots behind application-owned ports, and add one explicit SQLite migration. Presentation will call use cases only; weight-only entries will persist a measurement and invoke the existing calculation use cases using the latest earlier extended sizes, while extended entries will keep the existing calculation pipeline.

**Tech Stack:** C#, .NET 10, .NET MAUI/XAML, CommunityToolkit.Mvvm, sqlite-net-pcl, xUnit, and the existing solution projects.

**Spec:** `docs/superpowers/specs/2026-09-09-profile-measurement-ux-design.md`

## Global Constraints

- New profiles must store valid height, age, and activity level; legacy profiles may remain incomplete until edited, but the app must never invent personal values.
- A `WeightOnly` measurement stores the new weight and, after an earlier extended measurement exists, creates results using that measurement's neck and abdomen values.
- A `WeightAndSizes` measurement requires both neck and abdomen, uses the saved profile settings, and creates the existing versioned results.
- Historical measurements and calculation results are immutable; editing profile settings must not recalculate them.
- The maximum number of profiles is four, enforced in `CreateProfile` and represented by a visible but disabled `Add profile` button at the limit.
- Zero profiles show only the centered empty-state create action; one to four profiles show the top `Add profile` action.
- User-facing dates use `dd/MM/yyyy`; persistence continues to store UTC timestamps.
- All visible copy must be friendly and must not expose property names, enum names, formula metadata, or implementation terminology.
- Domain and Application code must not reference MAUI, Android, XAML, or SQLite.
- Do not add a package, network service, account system, analytics SDK, settings implementation, help implementation, or graph screen.
- Follow red-green-refactor for each behavior and run the smallest relevant test before the production implementation.

---

### Task 1: Add validated profile settings and measurement types to Domain

**Files:**
- Create: `src/Anthropometry.Domain/Profiles/ProfileSettings.cs`
- Create: `src/Anthropometry.Domain/Measurements/MeasurementType.cs`
- Modify: `src/Anthropometry.Domain/Profiles/Profile.cs`
- Modify: `src/Anthropometry.Domain/Measurements/MeasurementInput.cs`
- Modify: `src/Anthropometry.Domain/Measurements/Measurement.cs`
- Test: `tests/Anthropometry.Domain.Tests/Profiles/ProfileTests.cs`
- Test: `tests/Anthropometry.Domain.Tests/Measurements/MeasurementTests.cs`

**Interfaces:**

```csharp
public sealed record ProfileSettings(decimal HeightCm, int AgeYears, ActivityLevel ActivityLevel)
{
    public static Result<ProfileSettings> Create(decimal heightCm, int ageYears, ActivityLevel activityLevel);
}

public enum MeasurementType
{
    WeightOnly = 1,
    WeightAndSizes = 2
}

public sealed record MeasurementInput(
    MeasurementType Type,
    decimal WeightKg,
    decimal HeightCm,
    decimal? NeckCm,
    decimal? AbdomenCm,
    int AgeYears,
    ActivityLevel ActivityLevel,
    DateTimeOffset MeasuredAtUtc);
```

`Profile.Settings` is nullable only to represent a legacy database row that has not yet been completed. `Profile.Create` and `Profile.Update` require valid settings; `Profile.Rehydrate` accepts nullable settings for migration compatibility. Preserve `ProfileId`, timestamps, and name validation.

- [ ] **Step 1: Write failing profile-settings tests**

Add tests named `Settings_rejects_height_outside_metric_range`, `Settings_rejects_age_outside_range`, `Settings_rejects_unknown_activity_level`, and `Rehydrate_allows_missing_settings_for_legacy_profiles`. Assert the error codes `profile.settings.height.invalid`, `profile.settings.age.invalid`, and `profile.settings.activity.invalid` for invalid values, and assert that valid settings retain `180m`, `35`, and `ActivityLevel.Moderate`.

- [ ] **Step 2: Run the focused profile tests and verify the failure**

```powershell
dotnet test tests/Anthropometry.Domain.Tests --configuration Release --filter FullyQualifiedName~ProfileTests
```

Expected: compilation or assertion failures because `ProfileSettings` and the new profile APIs do not exist yet.

- [ ] **Step 3: Implement `ProfileSettings` and extend `Profile`**

Reuse the existing height, age, activity, timestamp, and required-name validation ranges. Implement these methods:

```csharp
public static Result<Profile> Create(string? name, ProfileSettings settings, DateTimeOffset createdAtUtc);
public static Result<Profile> Rehydrate(
    ProfileId id,
    string? name,
    ProfileSettings? settings,
    DateTimeOffset createdAtUtc,
    DateTimeOffset updatedAtUtc);
public Result Update(string? name, ProfileSettings settings, DateTimeOffset updatedAtUtc);
```

Do not add a second settings service or a generic value-object framework.

- [ ] **Step 4: Write failing measurement-type tests**

Add tests named `Create_weight_only_accepts_missing_sizes`, `Create_weight_only_rejects_supplied_sizes`, `Create_weight_and_sizes_requires_both_sizes`, `Create_weight_and_sizes_validates_size_ranges`, and `Create_rejects_unknown_measurement_type`. Continue asserting that height, age, activity, UTC timestamps, and weight are validated for both types.

- [ ] **Step 5: Run the focused measurement tests and verify the failure**

```powershell
dotnet test tests/Anthropometry.Domain.Tests --configuration Release --filter FullyQualifiedName~MeasurementTests
```

Expected: compilation failures because `MeasurementType` and nullable size fields are not implemented.

- [ ] **Step 6: Implement the measurement type and validation**

Make `Measurement.NeckCm` and `Measurement.AbdomenCm` nullable. `WeightOnly` must have both sizes null; `WeightAndSizes` must have both values present and in the existing ranges. Keep height, age, and activity as non-null measurement snapshots so every newly recorded measurement has the profile context used at capture time.

- [ ] **Step 7: Run all Domain tests and commit**

```powershell
dotnet test tests/Anthropometry.Domain.Tests --configuration Release
git add src/Anthropometry.Domain tests/Anthropometry.Domain.Tests
git commit -m "feat: add profile settings and measurement types"
```

Expected: all Domain tests pass, including existing formula tests.

---

### Task 2: Extend Application profile contracts and enforce four profiles

**Files:**
- Modify: `src/Anthropometry.Application/Common/ApplicationErrors.cs`
- Modify: `src/Anthropometry.Application/Common/ApplicationModels.cs`
- Modify: `src/Anthropometry.Application/Profiles/CreateProfile.cs`
- Delete: `src/Anthropometry.Application/Profiles/RenameProfile.cs`
- Create: `src/Anthropometry.Application/Profiles/UpdateProfile.cs`
- Test: `tests/Anthropometry.Application.Tests/Profiles/ProfileUseCaseTests.cs`
- Modify: `tests/Anthropometry.Application.Tests/Support/Fakes.cs`

**Interfaces:**

```csharp
public sealed record ProfileSettingsInput(decimal HeightCm, int AgeYears, ActivityLevel ActivityLevel);

public sealed record CreateProfileCommand(string Name, ProfileSettingsInput Settings);

public sealed record ProfileSettingsDto(decimal HeightCm, int AgeYears, ActivityLevel ActivityLevel);

public sealed record ProfileDto(
    ProfileId Id,
    string Name,
    ProfileSettingsDto? Settings,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record MeasurementDto(
    MeasurementId Id,
    ProfileId ProfileId,
    MeasurementType Type,
    DateTimeOffset MeasuredAtUtc,
    decimal WeightKg,
    decimal HeightCm,
    decimal? NeckCm,
    decimal? AbdomenCm,
    int AgeYears,
    ActivityLevel ActivityLevel);

public sealed class UpdateProfile
{
    public Task<Result<ProfileDto>> ExecuteAsync(
        ProfileId id,
        string name,
        ProfileSettingsInput settings,
        CancellationToken cancellationToken);
}
```

`CreateProfile.ExecuteAsync` becomes `ExecuteAsync(CreateProfileCommand command, CancellationToken cancellationToken)`. Before creating a profile, load the existing list and return `ApplicationErrors.ProfileLimitReached` when its count is four or greater. Keep persistence failures translated to `PersistenceUnavailable`. `UpdateProfile` replaces the old rename-only use case and updates name plus settings through `Profile.Update`. `ApplicationModels.ToDto` copies `Measurement.Type` and nullable size fields without changing stored values.

- [ ] **Step 1: Write failing application tests for the new profile contract**

Add `CreateProfile_rejects_the_fifth_profile`, `CreateProfile_persists_name_and_settings`, `UpdateProfile_changes_name_and_settings`, `UpdateProfile_returns_not_found_for_missing_profile`, and `CreateProfile_returns_settings_validation_error`. Assert the fifth-create error code is `profile.limit.reached`, and assert the fake repository contains exactly four profiles after the rejected call.

- [ ] **Step 2: Run the focused application profile tests and verify the failure**

```powershell
dotnet test tests/Anthropometry.Application.Tests --configuration Release --filter FullyQualifiedName~ProfileUseCaseTests
```

Expected: compilation failures because the command, DTO, settings, and update use case do not exist.

- [ ] **Step 3: Implement DTO mapping, errors, create-limit enforcement, and update**

Use `const int MaxProfiles = 4` inside `CreateProfile`. Map nullable legacy settings to `ProfileSettingsDto?`; do not map a missing setting to zero or a made-up activity level. Update all application test data helpers to create a valid profile with `ProfileSettings.Create(180m, 35, ActivityLevel.Moderate)`.

- [ ] **Step 4: Run the full Application test suite**

```powershell
dotnet test tests/Anthropometry.Application.Tests --configuration Release
```

Expected: all profile, measurement, and calculation tests pass after their constructors are updated to the new profile and measurement contracts.

- [ ] **Step 5: Commit the Application profile slice**

```powershell
git add src/Anthropometry.Application tests/Anthropometry.Application.Tests
git commit -m "feat: persist profile settings and limit profiles"
```

---

### Task 3: Migrate SQLite and update repository mappings

**Files:**
- Modify: `src/Anthropometry.Infrastructure/Persistence/Sqlite/SqliteSchema.cs`
- Modify: `src/Anthropometry.Infrastructure/Persistence/Migrations/MigrationRunner.cs`
- Create: `src/Anthropometry.Infrastructure/Persistence/Migrations/Migration0002.cs`
- Modify: `src/Anthropometry.Infrastructure/Repositories/SqliteProfileRepository.cs`
- Modify: `src/Anthropometry.Infrastructure/Repositories/SqliteMeasurementRepository.cs`
- Test: `tests/Anthropometry.Infrastructure.Tests/Persistence/MigrationTests.cs`
- Modify: `tests/Anthropometry.Infrastructure.Tests/Persistence/MigrationUpgradeTests.cs`
- Modify: `tests/Anthropometry.Infrastructure.Tests/Repositories/SqliteProfileRepositoryTests.cs`
- Modify: `tests/Anthropometry.Infrastructure.Tests/Repositories/SqliteMeasurementRepositoryTests.cs`
- Modify: `tests/Anthropometry.Infrastructure.Tests/Support/TestData.cs`

**Interfaces:**

`SqliteSchema.CurrentVersion` becomes `2`. `Migration0002.Version` is `2`. Repository SQL must round-trip `Profile.Settings`, `Measurement.Type`, and nullable `NeckCm`/`AbdomenCm` without leaking SQLite row types outside Infrastructure.

- [ ] **Step 1: Write the migration upgrade test before implementing version 2**

Replace the fixture migration in `MigrationUpgradeTests` with a real upgrade scenario. Create a schema-version-1 database, insert one profile, one legacy measurement, and one calculation result, then run `new MigrationRunner(factory)` and assert:

```csharp
Assert.Equal("2", version);
Assert.Equal(profile.Id, loadedProfile!.Id);
Assert.Equal(MeasurementType.WeightAndSizes, loadedMeasurement!.Type);
Assert.Equal(40m, loadedMeasurement.NeckCm);
Assert.Equal(90m, loadedMeasurement.AbdomenCm);
Assert.Single(loadedResults);
```

Also assert that a profile with no measurements has `Settings == null` after migration.

- [ ] **Step 2: Run the migration tests and verify the failure**

```powershell
dotnet test tests/Anthropometry.Infrastructure.Tests --configuration Release --filter FullyQualifiedName~Migration
```

Expected: the schema remains at version 1 and the new fields cannot be read.

- [ ] **Step 3: Implement `Migration0002`**

Add nullable `HeightCm`, `AgeYears`, and `ActivityLevel` columns to `Profiles`. Rebuild `Measurements` with `MeasurementType INTEGER NOT NULL`, nullable `NeckCm` and `AbdomenCm`, and the existing snapshot fields. Copy every old measurement with `MeasurementType = 2`, preserve IDs/timestamps/values, and rebuild `CalculationResults` while foreign keys are temporarily disabled so its existing rows continue to reference the rebuilt Measurements table. Recreate the existing indexes and re-enable foreign keys before the migration transaction completes.

Backfill each profile's settings from its newest measurement using the same `ProfileId` and `MeasuredAtUtc` ordering used by history. Leave settings null when no measurement exists.

- [ ] **Step 4: Register the complete default migration list**

Change the empty-registration fallback in `MigrationRunner` to `[new Migration0001(), new Migration0002()]`, and register both migrations explicitly in `MauiProgram` as `IMigration` services. Preserve the regression behavior where an empty dependency enumeration still initializes the default schema.

- [ ] **Step 5: Update repository SQL and mappings**

`SqliteProfileRepository` must select, insert, and update the three profile setting columns and call `Profile.Rehydrate` with nullable settings. `SqliteMeasurementRepository` must select and write `MeasurementType`, pass nullable size values to `MeasurementInput`, and map null database values to null domain values. Keep timestamps through `SqliteValueConverter` so they remain UTC.

- [ ] **Step 6: Add repository round-trip tests**

Add `Add_and_get_profile_round_trips_settings`, `Add_and_get_weight_only_measurement_round_trips_missing_sizes`, and `Upgrade_preserves_calculation_results`. Verify that the database contains null for weight-only size columns, `MeasurementType` is `1` for weight-only and `2` for extended, and all existing result formula metadata is unchanged.

- [ ] **Step 7: Run infrastructure tests and commit**

```powershell
dotnet test tests/Anthropometry.Infrastructure.Tests --configuration Release
git add src/Anthropometry.Infrastructure tests/Anthropometry.Infrastructure.Tests
git commit -m "feat: migrate sqlite for profile settings and measurement types"
```

Expected: fresh databases start at version 2, version-1 databases upgrade without losing rows, and all repository tests pass.

---

### Task 4: Record profile-backed measurements and reuse prior sizes for weight-only entries

**Files:**
- Modify: `src/Anthropometry.Application/Common/ApplicationErrors.cs`
- Modify: `src/Anthropometry.Application/Measurements/RecordMeasurement.cs`
- Modify: `src/Anthropometry.Application/Calculations/CalculateBodyFat.cs`
- Modify: `src/Anthropometry.Application/Calculations/CalculateBasalMetabolicRate.cs`
- Modify: `src/Anthropometry.Application/Calculations/CalculateTotalDailyEnergyExpenditure.cs`
- Modify: `tests/Anthropometry.Application.Tests/Measurements/MeasurementUseCaseTests.cs`
- Modify: `tests/Anthropometry.Application.Tests/Calculations/CalculationUseCaseTests.cs`
- Modify: `tests/Anthropometry.Application.Tests/Support/Fakes.cs`

**Interfaces:**

```csharp
public sealed record RecordMeasurementCommand(
    ProfileId ProfileId,
    MeasurementType Type,
    decimal WeightKg,
    decimal? NeckCm,
    decimal? AbdomenCm,
    DateTimeOffset MeasuredAtUtc);
```

`RecordMeasurement` loads the profile, requires `Profile.Settings` to be complete, creates the `MeasurementInput` from the saved height/age/activity plus command weight/sizes, validates it through Domain, and persists it. The UI cannot supply a different height, age, or activity for a measurement. Add `ApplicationErrors.ProfileSettingsRequired` and `ApplicationErrors.CalculationUnavailableForMeasurementType`.

All three calculation use cases must return `CalculationUnavailableForMeasurementType` when the target measurement is `WeightOnly` and no earlier extended measurement can supply sizes. When one exists, the use cases calculate using the target measurement's weight/profile snapshot and the earlier measurement's neck and abdomen values. `CalculateBodyFat` must also return a controlled domain failure when no usable size source exists instead of dereferencing nullable values.

- [ ] **Step 1: Write failing use-case tests**

Add `RecordMeasurement_weight_only_uses_profile_settings_and_persists_no_sizes`, `RecordMeasurement_extended_uses_profile_settings`, `RecordMeasurement_rejects_incomplete_profile_settings`, and calculation tests for both rejected weight-only entries without prior sizes and successful weight-only entries using the latest earlier extended measurement. For the weight-only test, assert the saved measurement has profile height `180m`, age `35`, moderate activity, null neck/abdomen, and type `WeightOnly`.

- [ ] **Step 2: Run the focused application tests and verify the failure**

```powershell
dotnet test tests/Anthropometry.Application.Tests --configuration Release --filter "FullyQualifiedName~MeasurementUseCaseTests|FullyQualifiedName~CalculationUseCaseTests"
```

Expected: compilation or assertion failures because the command no longer carries raw profile settings and calculation guards do not exist.

- [ ] **Step 3: Implement profile-backed recording and calculation guards**

Keep the existing repository and formula interfaces. Do not create a measurement service or coordinator. The existing three calculation use cases remain the only calculation entry points and resolve an earlier extended measurement when the target is `WeightOnly`.

- [ ] **Step 4: Assert that weight-only recording recalculates from the latest earlier sizes**

Use the fake repositories to record a weight-only measurement after an older extended measurement. Assert that three new result rows are attached to the new measurement and the older result rows remain present and attached to the older measurement ID.

- [ ] **Step 5: Run the full Application tests and commit**

```powershell
dotnet test tests/Anthropometry.Application.Tests --configuration Release
git add src/Anthropometry.Application tests/Anthropometry.Application.Tests
git commit -m "feat: record profile-backed measurement types"
```

---

### Task 5: Add friendly profile editing, limit state, and toolbar actions

**Files:**
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileEditorViewModel.cs`
- Create: `src/Anthropometry.App/Features/Profiles/ActivityLevelOption.cs`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileEditorPage.xaml`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileListViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileListPage.xaml`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileDetailViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileDetailPage.xaml`
- Modify: `src/Anthropometry.App/MauiNavigation.cs`
- Modify: `src/Anthropometry.App/MauiProgram.cs`
- Modify: `tests/Anthropometry.App.Tests/Features/Profiles/ProfileEditorViewModelTests.cs`
- Modify: `tests/Anthropometry.App.Tests/Features/Profiles/ProfileListViewModelTests.cs`
- Modify: `tests/Anthropometry.App.Tests/ResponsiveLayoutTests.cs`
- Modify: test navigation spies in `tests/Anthropometry.App.Tests/Support/Fakes.cs` and the feature test files

**Interfaces:**

`ProfileEditorViewModel` consumes `CreateProfile` and `UpdateProfile`, exposes `Name`, `HeightText`, `AgeText`, a friendly `ActivityLevelOption` list, `SelectedActivityLevel`, `Title`, `ValidationMessage`, `ErrorMessage`, and `SaveCommand`. It parses and validates the three settings before creating or updating a profile.

`ProfileListViewModel` exposes `HasProfiles`, `CanAddProfile`, `IsEmpty`, `CreateCommand`, `SettingsCommand`, and `HelpCommand`. `HasProfiles` is true when the loaded collection is non-empty. `CanAddProfile` is true only when the loaded profile count is below four. `CreateCommand` must be disabled when `CanAddProfile` is false, and its `NotifyCanExecuteChanged` must run after every successful load or delete.

Define the presentation-only picker option as:

```csharp
public sealed record ActivityLevelOption(ActivityLevel Value, string DisplayName);
```

- [ ] **Step 1: Write failing ViewModel tests for settings and list state**

Add `Save_rejects_invalid_profile_settings`, `Save_creates_profile_with_settings`, `Save_updates_existing_profile_settings`, `Load_enables_add_profile_below_limit`, `Load_disables_add_profile_at_four_profiles`, and `Empty_state_is_the_only_create_action_when_no_profiles_exist`. Assert `CanAddProfile` is false at four, the command cannot execute at four, and remains true at zero through three.

- [ ] **Step 2: Run the focused App tests and verify the failure**

```powershell
dotnet test tests/Anthropometry.App.Tests --configuration Release --filter "FullyQualifiedName~ProfileEditorViewModelTests|FullyQualifiedName~ProfileListViewModelTests"
```

Expected: compilation failures because the editor settings properties and list limit properties do not exist.

- [ ] **Step 3: Implement editor and list ViewModel behavior**

Use the existing current/invariant decimal parsing approach. Map activity levels to user-facing labels such as `Sedentary`, `Lightly active`, `Moderately active`, `Highly active`, and `Very highly active`; never bind the enum directly to a picker. Translate `profile.limit.reached` to a friendly non-technical message, while retaining the disabled button as the primary indication.

- [ ] **Step 4: Update profile pages and navigation**

The profile editor must show friendly labels for name, height (`cm`), age (`years`), and activity level. The create title is `Create profile`; the edit title is `Edit profile`. Update `MauiNavigation` to construct `CreateProfile`/`UpdateProfile` and pass the selected profile settings into the editor.

On the profile list:

```xml
<Button Text="Add profile"
        Command="{Binding CreateCommand}"
        IsVisible="{Binding HasProfiles}"
        IsEnabled="{Binding CanAddProfile}" />
```

Keep the centered empty-state action for zero profiles and use friendly labels such as `Create profile`, `Open profile`, and `Delete profile`. Add top toolbar items with no-op commands:

```xml
<ToolbarItem Text="Settings" Command="{Binding SettingsCommand}" />
<ToolbarItem Text="Help" Command="{Binding HelpCommand}" />
```

The profile detail page uses `Edit profile`, not `Rename profile`.

- [ ] **Step 5: Run App tests, build the shared target, and commit**

```powershell
dotnet test tests/Anthropometry.App.Tests --configuration Release
dotnet build src/Anthropometry.App/Anthropometry.App.csproj -f net10.0 -c Release
git add src/Anthropometry.App tests/Anthropometry.App.Tests
git commit -m "feat: improve profile management ux"
```

Expected: profile ViewModel tests pass, the shared target builds, and the Add profile command is visibly disabled at the limit on Android.

---

### Task 6: Add separate weight and size-based measurement flows

**Files:**
- Modify: `src/Anthropometry.App/Features/Profiles/IProfileNavigation.cs`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileDetailViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileDetailPage.xaml`
- Modify: `src/Anthropometry.App/Features/Measurements/IMeasurementNavigation.cs`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementEditorViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementEditorPage.xaml`
- Modify: `src/Anthropometry.App/MauiNavigation.cs`
- Modify: `tests/Anthropometry.App.Tests/Features/Measurements/MeasurementEditorViewModelTests.cs`
- Modify: `tests/Anthropometry.App.Tests/ResponsiveLayoutTests.cs`

**Interfaces:**

```csharp
Task CreateMeasurementAsync(ProfileDto profile, MeasurementType type);
Task CloseMeasurementAsync();
Task ShowResultsAsync(MeasurementDto measurement);
```

`MeasurementEditorViewModel` receives the selected `ProfileDto` and `MeasurementType`. Its form exposes `WeightText`, and exposes `NeckText`/`AbdomenText` only for `WeightAndSizes`. It no longer exposes editable height, age, or activity fields. `CanSave` requires only valid weight for `WeightOnly`, and weight plus both valid sizes for `WeightAndSizes`.

- [ ] **Step 1: Write failing ViewModel tests for both methods**

Add `WeightOnly_save_requires_only_weight`, `WeightOnly_save_recalculates_from_previous_sizes`, `WeightAndSizes_save_requires_both_sizes`, `WeightAndSizes_save_calculates_three_results`, and `WeightOnly_save_does_not_navigate_to_results`. Assert that the recorded command has `MeasurementType.WeightOnly`, null sizes for the standard flow, and both size values for the extended flow.

- [ ] **Step 2: Run the focused measurement App tests and verify the failure**

```powershell
dotnet test tests/Anthropometry.App.Tests --configuration Release --filter FullyQualifiedName~MeasurementEditorViewModelTests
```

Expected: compilation failures because the editor constructor and navigation contract still require manually entered profile settings.

- [ ] **Step 3: Implement the two editor modes**

For weight-only success, call `RecordMeasurement`, run the existing three calculation use cases against the new weight and the latest earlier sizes, set `IsCompleted`, and call `CloseMeasurementAsync`. For weight-and-sizes success, call the same three calculation use cases, set `IsCompleted`, and call `CloseMeasurementAsync`; results remain available from History. Keep the current recoverable persistence and calculation error translation, but make validation mention the relevant fields instead of “all fields”.

- [ ] **Step 4: Add profile actions and navigation construction**

Replace `New measurement` with two friendly actions: `Add weight` and `Add measurements`. `MauiNavigation.CreateMeasurementAsync` passes the profile and selected `MeasurementType` to the editor. `CloseMeasurementAsync` pops the editor after either successful save.

- [ ] **Step 5: Update XAML and responsive tests**

The weight form contains one metric row (`Weight`, `kg`). The size form contains `Weight`, `Neck`, and `Abdomen`, each with `cm` where applicable. Keep the medical-estimate disclaimer, minimum touch targets, scrolling, and keyboard behavior. Do not render height, age, or activity as editable measurement fields.

- [ ] **Step 6: Run App tests, build Android, and commit**

```powershell
dotnet test tests/Anthropometry.App.Tests --configuration Release
dotnet build src/Anthropometry.App/Anthropometry.App.csproj -f net10.0-android -c Debug
git add src/Anthropometry.App tests/Anthropometry.App.Tests
git commit -m "feat: add weight-only and extended measurements"
```

Expected: both measurement journeys pass in ViewModel tests and the Android target builds.

---

### Task 7: Show warning state, history labels, and normalized dates

**Files:**
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileDetailViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileDetailPage.xaml`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementHistoryViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Measurements/MeasurementHistoryPage.xaml`
- Modify: `src/Anthropometry.App/App.xaml.cs`
- Create: `tests/Anthropometry.App.Tests/Features/Profiles/ProfileDetailViewModelTests.cs`
- Modify: `tests/Anthropometry.App.Tests/Features/Measurements/MeasurementHistoryViewModelTests.cs`

**Interfaces:**

Add a presentation-only history item so formatting and button visibility are testable without parsing XAML:

```csharp
public sealed record MeasurementHistoryItem(
    MeasurementDto Measurement,
    string DateText,
    string MeasurementTypeText)
{
    public bool CanViewResults => Measurement.Type is MeasurementType.WeightOnly or MeasurementType.WeightAndSizes;
}
```

`MeasurementHistoryViewModel.Measurements` becomes a read-only collection of `MeasurementHistoryItem`; both supported measurement types can open their results. `ProfileDetailViewModel` consumes `GetMeasurementHistory`, exposes `LoadCommand`, and exposes `CanAddWeight`, which is true after any extended measurement exists.

- [ ] **Step 1: Write failing warning and formatting tests**

Create `ProfileDetailViewModelTests` covering disabled `Add weight` with no extended history and enabled `Add weight` after an extended measurement. Add history assertions that `DateText` equals `08/09/2026` for a UTC timestamp on that date, `MeasurementTypeText` is friendly, and both measurement types can view results.

- [ ] **Step 2: Run the focused App tests and verify the failure**

```powershell
dotnet test tests/Anthropometry.App.Tests --configuration Release --filter "FullyQualifiedName~ProfileDetailViewModelTests|FullyQualifiedName~MeasurementHistoryViewModelTests"
```

Expected: compilation failures because the profile detail query, history item, and formatted date do not exist.

- [ ] **Step 3: Implement the weight-entry prerequisite and results warning state**

Load history on an explicit `LoadCommand` or the existing page lifecycle hook, keep loading/error state recoverable, and enable `Add weight` only when an extended measurement exists. Render the warning icon on the results page for weight-only entries with an accessibility description; do not add the future advice text yet.

- [ ] **Step 4: Implement history presentation items**

Format dates using `DateTimeOffset.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture)`. Use friendly labels `Weight only` and `Weight and sizes`. Show `View results` for both supported measurement types, and keep weight and available sizes readable with units.

- [ ] **Step 5: Normalize remaining visible copy**

Replace any remaining raw or awkward labels in the changed screens and startup error UI. Use `Create profile`, `Add profile`, `Add weight`, `Add measurements`, `Edit profile`, `Measurement history`, `View results`, `Settings`, and `Help`. Keep the existing result-card behavior: friendly calculation titles, exactly two decimals, value, and unit only; do not reintroduce formula IDs or versions into the visible result cards.

- [ ] **Step 6: Run App tests and commit**

```powershell
dotnet test tests/Anthropometry.App.Tests --configuration Release
git add src/Anthropometry.App tests/Anthropometry.App.Tests
git commit -m "feat: show measurement freshness and friendly history"
```

---

### Task 8: Update architecture documentation and perform release verification

**Files:**
- Modify: `ARCHITECTURE.md`
- Modify: `PLAN.md`
- Create: `docs/testing/profile-measurement-acceptance.md`

- [ ] **Step 1: Update architecture decisions**

Document that profiles persist height, age, and activity; measurements have `WeightOnly` and `WeightAndSizes` types; size fields may be absent only for weight-only measurements; each measurement keeps a profile-setting snapshot; weight-only results reuse the latest earlier extended sizes; and all dates stay stored as UTC and all personal data local.

- [ ] **Step 2: Update `PLAN.md` acceptance tracking**

Add a clearly labeled post-MVP feature slice with checked acceptance criteria only after verification. Record the four-profile limit, disabled button behavior, two measurement flows, warning icon behavior, `dd/MM/yyyy` history dates, and visible Settings/Help placeholders.

- [ ] **Step 3: Write the Android acceptance checklist**

In `docs/testing/profile-measurement-acceptance.md`, list these manual checks: first launch with zero profiles shows only the centered create action; creating profiles 1–3 keeps enabled `Add profile`; profile 4 leaves `Add profile` visible but disabled; profile editing saves height/age/activity; `Add weight` is disabled until an extended measurement exists; `Add measurements` calculates the three results; a later weight-only entry returns to the profile, keeps the profile warning-free, and exposes recalculated results with a warning icon in History; history dates display `dd/MM/yyyy`; Settings and Help appear in the top bar and do nothing; reopening the app preserves local data.

- [ ] **Step 4: Run the complete verification commands**

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
dotnet build src/Anthropometry.App/Anthropometry.App.csproj -f net10.0-android -c Release
```

Expected: all commands succeed, the Android build has no new errors, and all tests pass.

- [ ] **Step 5: Review the final diff**

Run:

```powershell
git diff --check
git status --short
git diff --stat HEAD~8..HEAD
```

Confirm that no credentials, signing keys, personal sample databases, network permissions, external packages, raw measurements in logs, or unrelated files were added. Confirm that every changed presentation path uses Application use cases rather than repositories directly.

- [ ] **Step 6: Commit documentation and verification**

```powershell
git add ARCHITECTURE.md PLAN.md README.md docs/testing/profile-measurement-acceptance.md
git commit -m "docs: record profile measurement ux acceptance"
```

## Plan self-review

- Domain validation, profile settings, measurement modes, and immutable snapshots are covered by Task 1.
- The four-profile rule is covered in Application and Presentation by Tasks 2 and 5.
- The migration preserves legacy profiles, measurements, and calculation results in Task 3.
- Weight-only entries create new results from the latest earlier sizes and never mutate old results in Task 4 and Task 6.
- The warning icon, friendly labels, and `dd/MM/yyyy` formatting are covered by Task 7.
- Settings and Help placeholders and the zero-profile/top-button behavior are covered by Task 5.
- Android build, full tests, privacy review, architecture documentation, and acceptance criteria are covered by Task 8.
- No task introduces an unspecified package, service, backend, or platform dependency.
