# Anthropometric Tracking App Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an Android-first, offline-first mobile app that stores multiple profiles and measurements in SQLite and calculates versioned body-fat, BMR, and TDEE results.

**Architecture:** Use .NET MAUI with C# and XAML/MVVM for the Android application, with domain-independent Domain, Application, Infrastructure, and Presentation layers. Keep formulas and use cases independent from MAUI and SQLite, and persist both raw measurements and versioned calculation results.

**Tech Stack:** .NET 10, .NET MAUI, C#, XAML, `CommunityToolkit.Mvvm`, `sqlite-net-pcl`, SQLite, xUnit, Android emulator or device.

**Spec:** `ARCHITECTURE.md`

## Global Constraints

- Initial platform: Android-first.
- Framework: .NET MAUI on .NET 10.
- Language: C#.
- UI: .NET MAUI with XAML and MVVM.
- MVVM state: `CommunityToolkit.Mvvm`.
- Database: Local SQLite in the app's private storage.
- Initial SQLite access: `sqlite-net-pcl`, hidden behind application-owned interfaces.
- Architecture: Pragmatic Clean Architecture with domain-oriented boundaries.
- Testing: TDD for domain and application; infrastructure tests and critical UI-flow tests.
- Network: Out of scope for the MVP; no backend or synchronization.
- Initial units: Metric system.
- Store `AgeYears` on each measurement so the age used by historical calculations is preserved.
- The male body-fat formula is the US Navy equation and converts centimeters to inches before calculation; the female body-fat formula is the versioned US Navy/Hodgdon-Beckett equation using waist, hip, neck, and height.
- Male and female BMR use their corresponding Mifflin-St Jeor variants.
- TDEE is the gender-specific BMR multiplied by a named activity level and its defined factor; no separate female TDEE equation is introduced.
- Every formula has a stable identity and version.
- UI code never accesses SQLite directly.
- Domain code never references MAUI, Android, XAML, or SQLite.
- All user-facing Markdown documentation is written in English.

## Current implementation status

Slices 0 through 7 are implemented with automated Domain, Application, Infrastructure, and plain `net10.0` Presentation verification. The approved profile-settings and measurement-modes feature slice is implemented and tracked below. The Android target is configured as `net10.0-android`; profile CSV transfer is implemented as a post-MVP offline feature and its manual Android smoke pass remains pending.

The App project also targets plain `net10.0` for ViewModel tests; MAUI pages and platform files remain Android-only. SQLitePCLRaw transitive packages are pinned to version `2.1.13` because the version selected by `sqlite-net-pcl` `1.9.172` is reported by NuGet as vulnerable.

## Slice dependency map

```text
Slice 0: repository and solution bootstrap
        │
        ├── Slice 1: domain model and validation
        │       │
        │       └── Slice 2: calculation formulas
        │
        ├── Slice 3: SQLite schema and repositories
        │
        └── Slice 4: application use cases
                │
                ├── Slice 5: profile UI
                ├── Slice 6: measurement and result UI
                └── Slice 7: history, error states, and responsive hardening
                        │
                        └── Slice 8: release verification
```

Each slice ends with a build/test checkpoint and a reviewable product increment.

---

### Slice 0: Solution and repository bootstrap

**Review boundary:** The repository contains a buildable Android MAUI shell, independent class libraries, and empty test projects with dependency directions enforced.

**Files:**

- Create: `Anthropometry.sln`
- Create: `global.json`
- Create: `Directory.Build.props`
- Create: `Directory.Packages.props`
- Create: `src/Anthropometry.Domain/Anthropometry.Domain.csproj`
- Create: `src/Anthropometry.Application/Anthropometry.Application.csproj`
- Create: `src/Anthropometry.Infrastructure/Anthropometry.Infrastructure.csproj`
- Create: `src/Anthropometry.App/Anthropometry.App.csproj`
- Create: `tests/Anthropometry.Domain.Tests/Anthropometry.Domain.Tests.csproj`
- Create: `tests/Anthropometry.Application.Tests/Anthropometry.Application.Tests.csproj`
- Create: `tests/Anthropometry.Infrastructure.Tests/Anthropometry.Infrastructure.Tests.csproj`
- Create: `tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj`
- Create: `src/Anthropometry.App/MauiProgram.cs`
- Create: `src/Anthropometry.App/App.xaml`
- Create: `src/Anthropometry.App/App.xaml.cs`
- Create: `src/Anthropometry.App/AppShell.xaml`
- Create: `src/Anthropometry.App/AppShell.xaml.cs`

**Interfaces:**

- Produces a .NET 10 MAUI app targeting `net10.0-android`.
- Produces class libraries targeting `net10.0`.
- Domain has no project reference to Application, Infrastructure, or App.
- Application references Domain.
- Infrastructure references Application and Domain.
- App references Application and Infrastructure.
- Test projects reference only the production projects they test.

- [ ] **Step 1: Create the solution and projects**

Run:

```powershell
dotnet new sln -n Anthropometry
dotnet new classlib -n Anthropometry.Domain -o src/Anthropometry.Domain -f net10.0
dotnet new classlib -n Anthropometry.Application -o src/Anthropometry.Application -f net10.0
dotnet new classlib -n Anthropometry.Infrastructure -o src/Anthropometry.Infrastructure -f net10.0
dotnet new maui -n Anthropometry.App -o src/Anthropometry.App
dotnet new xunit -n Anthropometry.Domain.Tests -o tests/Anthropometry.Domain.Tests -f net10.0
dotnet new xunit -n Anthropometry.Application.Tests -o tests/Anthropometry.Application.Tests -f net10.0
dotnet new xunit -n Anthropometry.Infrastructure.Tests -o tests/Anthropometry.Infrastructure.Tests -f net10.0
dotnet new xunit -n Anthropometry.App.Tests -o tests/Anthropometry.App.Tests -f net10.0
dotnet sln Anthropometry.sln add src/Anthropometry.Domain/Anthropometry.Domain.csproj
dotnet sln Anthropometry.sln add src/Anthropometry.Application/Anthropometry.Application.csproj
dotnet sln Anthropometry.sln add src/Anthropometry.Infrastructure/Anthropometry.Infrastructure.csproj
dotnet sln Anthropometry.sln add src/Anthropometry.App/Anthropometry.App.csproj
dotnet sln Anthropometry.sln add tests/Anthropometry.Domain.Tests/Anthropometry.Domain.Tests.csproj
dotnet sln Anthropometry.sln add tests/Anthropometry.Application.Tests/Anthropometry.Application.Tests.csproj
dotnet sln Anthropometry.sln add tests/Anthropometry.Infrastructure.Tests/Anthropometry.Infrastructure.Tests.csproj
dotnet sln Anthropometry.sln add tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj
```

- [ ] **Step 2: Add only the allowed project references**

Run:

```powershell
dotnet add src/Anthropometry.Application/Anthropometry.Application.csproj reference src/Anthropometry.Domain/Anthropometry.Domain.csproj
dotnet add src/Anthropometry.Infrastructure/Anthropometry.Infrastructure.csproj reference src/Anthropometry.Application/Anthropometry.Application.csproj
dotnet add src/Anthropometry.Infrastructure/Anthropometry.Infrastructure.csproj reference src/Anthropometry.Domain/Anthropometry.Domain.csproj
dotnet add src/Anthropometry.App/Anthropometry.App.csproj reference src/Anthropometry.Application/Anthropometry.Application.csproj
dotnet add src/Anthropometry.App/Anthropometry.App.csproj reference src/Anthropometry.Infrastructure/Anthropometry.Infrastructure.csproj
dotnet add tests/Anthropometry.Domain.Tests/Anthropometry.Domain.Tests.csproj reference src/Anthropometry.Domain/Anthropometry.Domain.csproj
dotnet add tests/Anthropometry.Application.Tests/Anthropometry.Application.Tests.csproj reference src/Anthropometry.Application/Anthropometry.Application.csproj
dotnet add tests/Anthropometry.Infrastructure.Tests/Anthropometry.Infrastructure.Tests.csproj reference src/Anthropometry.Infrastructure/Anthropometry.Infrastructure.csproj
dotnet add tests/Anthropometry.App.Tests/Anthropometry.App.Tests.csproj reference src/Anthropometry.App/Anthropometry.App.csproj
```

- [ ] **Step 3: Pin the SDK and centralize package versions**

Set `global.json` to the installed .NET 10 SDK used by the project. Put all package versions in `Directory.Packages.props` and enable central package management. Add only `CommunityToolkit.Mvvm`, `sqlite-net-pcl`, and the test SDK packages required by the created test projects.

- [ ] **Step 4: Verify the empty solution**

Run:

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

Expected: restore, build, and all generated tests pass.

- [ ] **Step 5: Commit the bootstrap slice**

```powershell
git add Anthropometry.sln global.json Directory.Build.props Directory.Packages.props src tests
git commit -m "chore: bootstrap anthropometry app solution"
```

---

### Slice 1: Domain model and validation

**Review boundary:** Profiles, measurements, controlled values, and domain errors can be created and validated without MAUI, SQLite, or a network connection.

**Files:**

- Create: `src/Anthropometry.Domain/Common/DomainError.cs`
- Create: `src/Anthropometry.Domain/Common/Result.cs`
- Create: `src/Anthropometry.Domain/Common/Guard.cs`
- Create: `src/Anthropometry.Domain/Profiles/ProfileId.cs`
- Create: `src/Anthropometry.Domain/Profiles/Profile.cs`
- Create: `src/Anthropometry.Domain/Measurements/MeasurementId.cs`
- Create: `src/Anthropometry.Domain/Measurements/Measurement.cs`
- Create: `src/Anthropometry.Domain/Measurements/MeasurementInput.cs`
- Create: `src/Anthropometry.Domain/Calculations/ActivityLevel.cs`
- Create: `src/Anthropometry.Domain/Calculations/CalculationType.cs`
- Test: `tests/Anthropometry.Domain.Tests/Profiles/ProfileTests.cs`
- Test: `tests/Anthropometry.Domain.Tests/Measurements/MeasurementTests.cs`
- Test: `tests/Anthropometry.Domain.Tests/Common/ResultTests.cs`

**Interfaces:**

```csharp
public sealed record MeasurementInput(
    decimal WeightKg,
    decimal HeightCm,
    decimal NeckCm,
    decimal AbdomenCm,
    int AgeYears,
    ActivityLevel ActivityLevel,
    DateTimeOffset MeasuredAtUtc);
```

`Profile.Create(string name, DateTimeOffset createdAtUtc)` returns `Result<Profile>`.

`Measurement.Create(ProfileId profileId, MeasurementInput input, DateTimeOffset idTime)` returns `Result<Measurement>`.

- [ ] **Step 1: Write failing profile tests**

```csharp
[Fact]
public void Create_rejects_blank_name()
{
    var result = Profile.Create(" ", DateTimeOffset.UtcNow);

    Assert.False(result.IsSuccess);
    Assert.Equal("profile.name.required", result.Error!.Code);
}
```

- [ ] **Step 2: Run the focused test**

Run:

```powershell
dotnet test tests/Anthropometry.Domain.Tests --filter FullyQualifiedName~ProfileTests
```

Expected: FAIL because `Profile`, `Result`, and the domain error code do not exist.

- [ ] **Step 3: Implement profile identity, entity, result, and guard types**

Use immutable identifiers, a non-empty name, UTC timestamps, and a domain error containing a stable code and user-safe message key. Keep constructors private when invariants require factory methods.

- [ ] **Step 4: Write failing measurement validation tests**

Cover zero or negative weight, height, neck, abdomen, and age; missing activity level; invalid timestamps; and a valid metric input. Assert stable error codes such as `measurement.weight.invalid` and `measurement.age.invalid`.

- [ ] **Step 5: Implement measurement creation and activity types**

Store all raw metric inputs, the profile identifier, measurement timestamp, and stable measurement identifier. Store the named activity level, not only its numeric factor.

- [ ] **Step 6: Run the complete domain test project**

```powershell
dotnet test tests/Anthropometry.Domain.Tests --configuration Release
```

Expected: all domain tests pass without starting Android or SQLite.

- [ ] **Step 7: Commit the domain slice**

```powershell
git add src/Anthropometry.Domain tests/Anthropometry.Domain.Tests
git commit -m "feat: add validated anthropometry domain model"
```

---

### Slice 2: Versioned calculation formulas

**Review boundary:** The three MVP calculations are pure, unit-aware, versioned, and fully covered by deterministic tests.

**Files:**

- Create: `src/Anthropometry.Domain/Calculations/ICalculationFormula.cs`
- Create: `src/Anthropometry.Domain/Calculations/CalculationResultValue.cs`
- Create: `src/Anthropometry.Domain/Calculations/BodyFat/BodyFatInput.cs`
- Create: `src/Anthropometry.Domain/Calculations/BodyFat/UsNavyMaleBodyFatFormula.cs`
- Create: `src/Anthropometry.Domain/Calculations/Bmr/BmrInput.cs`
- Create: `src/Anthropometry.Domain/Calculations/Bmr/MifflinStJeorMaleBmrFormula.cs`
- Create: `src/Anthropometry.Domain/Calculations/Tdee/TdeeInput.cs`
- Create: `src/Anthropometry.Domain/Calculations/Tdee/ActivityFactorTable.cs`
- Create: `src/Anthropometry.Domain/Calculations/Tdee/TdeeFormula.cs`
- Test: `tests/Anthropometry.Domain.Tests/Calculations/BodyFat/UsNavyMaleBodyFatFormulaTests.cs`
- Test: `tests/Anthropometry.Domain.Tests/Calculations/Bmr/MifflinStJeorMaleBmrFormulaTests.cs`
- Test: `tests/Anthropometry.Domain.Tests/Calculations/Tdee/TdeeFormulaTests.cs`

**Interfaces:**

```csharp
public interface ICalculationFormula<in TInput, TResult>
{
    CalculationType Type { get; }
    string FormulaId { get; }
    string Version { get; }
    Result<TResult> Calculate(TInput input);
}
```

Concrete formulas use `CalculationResultValue` as `TResult`; it contains the decimal value, unit, formula identity, and formula version.

- [ ] **Step 1: Write the body-fat formula tests**

Test centimeter-to-inch conversion and the formula:

```text
86.010 × log10(abdomenInches - neckInches)
- 70.041 × log10(heightInches)
+ 36.76
```

Include a known valid case, zero and negative values, `abdomen <= neck`, and a check that the result reports the expected formula identity and version.

- [ ] **Step 2: Run the body-fat tests and verify failure**

```powershell
dotnet test tests/Anthropometry.Domain.Tests --filter FullyQualifiedName~UsNavyMaleBodyFatFormulaTests
```

Expected: FAIL because the formula and result type do not exist.

- [ ] **Step 3: Implement the body-fat formula**

Convert centimeters to inches only inside the formula boundary, reject invalid logarithm inputs, keep calculations at full decimal precision, and round only when constructing the display-ready result.

- [ ] **Step 4: Write and implement BMR tests**

Test the male Mifflin-St Jeor equation:

```text
10 × weightKg + 6.25 × heightCm - 5 × ageYears + 5
```

Cover valid input, invalid input, kcal/day unit, and formula version.

- [ ] **Step 5: Write and implement TDEE tests**

Test each activity factor: `1.2`, `1.375`, `1.55`, `1.725`, and `1.9`. Verify that TDEE is BMR multiplied by the selected named level and that an unknown level cannot be passed as an anonymous numeric factor.

- [ ] **Step 6: Run all domain tests**

```powershell
dotnet test tests/Anthropometry.Domain.Tests --configuration Release
```

Expected: all domain model and calculation tests pass.

- [ ] **Step 7: Commit the calculation slice**

```powershell
git add src/Anthropometry.Domain tests/Anthropometry.Domain.Tests
git commit -m "feat: add versioned anthropometric calculations"
```

---

### Slice 3: SQLite schema, migrations, and repositories

**Review boundary:** Profiles, measurements, and calculation results can be stored and queried through application-owned repository interfaces, with schema versioning and transactional profile deletion.

**Files:**

- Create: `src/Anthropometry.Application/Abstractions/IProfileRepository.cs`
- Create: `src/Anthropometry.Application/Abstractions/IMeasurementRepository.cs`
- Create: `src/Anthropometry.Application/Abstractions/ICalculationResultRepository.cs`
- Create: `src/Anthropometry.Infrastructure/Persistence/Sqlite/SqliteConnectionFactory.cs`
- Create: `src/Anthropometry.Infrastructure/Persistence/Sqlite/SqliteSchema.cs`
- Create: `src/Anthropometry.Infrastructure/Persistence/Migrations/IMigration.cs`
- Create: `src/Anthropometry.Infrastructure/Persistence/Migrations/MigrationRunner.cs`
- Create: `src/Anthropometry.Infrastructure/Persistence/Migrations/Migration0001.cs`
- Create: `src/Anthropometry.Infrastructure/Repositories/SqliteProfileRepository.cs`
- Create: `src/Anthropometry.Infrastructure/Repositories/SqliteMeasurementRepository.cs`
- Create: `src/Anthropometry.Infrastructure/Repositories/SqliteCalculationResultRepository.cs`
- Test: `tests/Anthropometry.Infrastructure.Tests/Persistence/MigrationTests.cs`
- Test: `tests/Anthropometry.Infrastructure.Tests/Repositories/SqliteProfileRepositoryTests.cs`
- Test: `tests/Anthropometry.Infrastructure.Tests/Repositories/SqliteMeasurementRepositoryTests.cs`
- Test: `tests/Anthropometry.Infrastructure.Tests/Repositories/SqliteCalculationResultRepositoryTests.cs`

**Interfaces:**

```csharp
public interface IProfileRepository
{
    Task<IReadOnlyList<Profile>> GetAllAsync(CancellationToken cancellationToken);
    Task<Profile?> GetByIdAsync(ProfileId id, CancellationToken cancellationToken);
    Task AddAsync(Profile profile, CancellationToken cancellationToken);
    Task UpdateAsync(Profile profile, CancellationToken cancellationToken);
    Task DeleteWithMeasurementsAsync(ProfileId id, CancellationToken cancellationToken);
}
```

Use equivalent repository contracts for measurements and calculation results. All methods accept cancellation tokens and return domain/application types rather than SQLite row types.

- [ ] **Step 1: Write migration tests**

Verify that a fresh temporary database creates `Profiles`, `Measurements`, `CalculationResults`, and `SchemaMetadata`, and records schema version `1`.

- [ ] **Step 2: Run migration tests and verify failure**

```powershell
dotnet test tests/Anthropometry.Infrastructure.Tests --filter FullyQualifiedName~MigrationTests
```

Expected: FAIL because the connection factory and migration runner do not exist.

- [ ] **Step 3: Add the SQLite package and connection factory**

Use the app-private database path supplied by MAUI at composition time. The factory must not be called by Domain or Application.

- [ ] **Step 4: Implement migration version 1**

Create the four tables from `ARCHITECTURE.md`, add foreign keys and indexes for profile and measurement queries, and enable foreign-key enforcement for each connection.

- [ ] **Step 5: Write repository tests**

Test insert, update, lookup, ordering by measurement date, missing identifiers, and transactional deletion of a profile with its measurements and results.

- [ ] **Step 6: Implement repositories and mappings**

Keep SQLite row models private to Infrastructure. Map UTC timestamps and named values explicitly. Use one transaction for profile deletion.

- [ ] **Step 7: Run infrastructure tests**

```powershell
dotnet test tests/Anthropometry.Infrastructure.Tests --configuration Release
```

Expected: migration, mapping, query, and transaction tests pass against temporary SQLite databases.

- [ ] **Step 8: Commit the persistence slice**

```powershell
git add src/Anthropometry.Application/Abstractions src/Anthropometry.Infrastructure tests/Anthropometry.Infrastructure.Tests
git commit -m "feat: add versioned sqlite persistence"
```

---

### Slice 4: Application use cases

**Review boundary:** The complete MVP workflow can be executed through application use cases using fake repositories, without starting MAUI or Android.

**Files:**

- Create: `src/Anthropometry.Application/Profiles/CreateProfile.cs`
- Create: `src/Anthropometry.Application/Profiles/RenameProfile.cs`
- Create: `src/Anthropometry.Application/Profiles/DeleteProfile.cs`
- Create: `src/Anthropometry.Application/Profiles/GetProfiles.cs`
- Create: `src/Anthropometry.Application/Measurements/RecordMeasurement.cs`
- Create: `src/Anthropometry.Application/Measurements/GetMeasurementHistory.cs`
- Create: `src/Anthropometry.Application/Calculations/CalculateBodyFat.cs`
- Create: `src/Anthropometry.Application/Calculations/CalculateBasalMetabolicRate.cs`
- Create: `src/Anthropometry.Application/Calculations/CalculateTotalDailyEnergyExpenditure.cs`
- Create: `src/Anthropometry.Application/Abstractions/IClock.cs`
- Create: `src/Anthropometry.Application/Abstractions/IFormulaCatalog.cs`
- Test: `tests/Anthropometry.Application.Tests/Profiles/ProfileUseCaseTests.cs`
- Test: `tests/Anthropometry.Application.Tests/Measurements/MeasurementUseCaseTests.cs`
- Test: `tests/Anthropometry.Application.Tests/Calculations/CalculationUseCaseTests.cs`

**Interfaces:**

```csharp
public sealed record CalculateBodyFatCommand(ProfileId ProfileId, MeasurementId MeasurementId);
public sealed record CalculateBmrCommand(ProfileId ProfileId, MeasurementId MeasurementId);
public sealed record CalculateTdeeCommand(ProfileId ProfileId, MeasurementId MeasurementId);
```

Each calculation use case loads the measurement, selects the formula by type, calculates the result, and persists the versioned result. It must not duplicate formula math.

- [ ] **Step 1: Write failing profile use-case tests**

Cover create, rename, list, and delete. Verify that profile deletion calls the transactional repository operation once and returns a controlled not-found error when the profile is missing.

- [ ] **Step 2: Implement profile use cases**

Inject repository and clock abstractions. Return application DTOs and controlled errors, not infrastructure exceptions.

- [ ] **Step 3: Write failing measurement tests**

Verify that `RecordMeasurement` checks the profile exists, creates a domain measurement, persists it, and does not persist invalid input.

- [ ] **Step 4: Implement measurement and history use cases**

Return measurements ordered newest first and keep persistence mapping outside Application.

- [ ] **Step 5: Write failing calculation tests**

Verify that each calculation loads a measurement, selects the correct formula, stores the formula identity and version, returns the result, and returns a controlled error for missing or invalid data.

- [ ] **Step 6: Implement formula catalog and calculation use cases**

Register formulas by `CalculationType`. Reject duplicate formula identities and ensure the selected formula's version is written to the result repository.

- [ ] **Step 7: Run application tests**

```powershell
dotnet test tests/Anthropometry.Application.Tests --configuration Release
```

Expected: all profile, measurement, history, and calculation use-case tests pass with fakes.

- [ ] **Step 8: Commit the application slice**

```powershell
git add src/Anthropometry.Application tests/Anthropometry.Application.Tests
git commit -m "feat: add offline application use cases"
```

---

### Slice 5: Profile management UI

**Review boundary:** A user can list profiles, create a profile, rename it, and delete it with confirmation using the MAUI Android app.

**Files:**

- Create: `src/Anthropometry.App/Features/Profiles/ProfileListPage.xaml`
- Create: `src/Anthropometry.App/Features/Profiles/ProfileListPage.xaml.cs`
- Create: `src/Anthropometry.App/Features/Profiles/ProfileListViewModel.cs`
- Create: `src/Anthropometry.App/Features/Profiles/ProfileDetailPage.xaml`
- Create: `src/Anthropometry.App/Features/Profiles/ProfileDetailPage.xaml.cs`
- Create: `src/Anthropometry.App/Features/Profiles/ProfileDetailViewModel.cs`
- Create: `src/Anthropometry.App/Features/Profiles/ProfileEditorPage.xaml`
- Create: `src/Anthropometry.App/Features/Profiles/ProfileEditorPage.xaml.cs`
- Create: `src/Anthropometry.App/Features/Profiles/ProfileEditorViewModel.cs`
- Create: `src/Anthropometry.App/Features/Profiles/ProfileRowView.xaml`
- Modify: `src/Anthropometry.App/AppShell.xaml`
- Modify: `src/Anthropometry.App/MauiProgram.cs`
- Test: `tests/Anthropometry.App.Tests/Features/Profiles/ProfileListViewModelTests.cs`
- Test: `tests/Anthropometry.App.Tests/Features/Profiles/ProfileEditorViewModelTests.cs`

**Interfaces:**

- ViewModels consume only Application use cases.
- `ProfileListViewModel` exposes an observable read-only profile collection, loading state, empty state, error state, create command, select command, and delete command.
- `ProfileEditorViewModel` exposes the editable name, validation message, save command, and cancel command.

- [ ] **Step 1: Write ViewModel tests for loading and empty states**

Assert that the list shows loading while the use case is running, displays an empty state for zero profiles, and exposes profiles after success.

- [ ] **Step 2: Implement the list ViewModel and page**

Use `ObservableObject`, `ObservableProperty`, and `AsyncRelayCommand` from `CommunityToolkit.Mvvm`. Keep navigation behind an injected navigation abstraction or a page-level adapter.

- [ ] **Step 3: Write create, rename, and delete tests**

Verify required-name validation, successful save, recoverable error display, confirmation before delete, and refresh after a successful mutation.

- [ ] **Step 4: Implement the editor, row, and delete flow**

Keep text and validation messages localizable. Do not put profile creation or deletion rules in XAML event handlers.

- [ ] **Step 5: Add responsive and accessibility styles**

Use shared resources for spacing, typography, colors, and touch targets. Verify the list and editor on a small phone width and a larger Android width.

- [ ] **Step 6: Run App tests and build Android**

```powershell
dotnet test tests/Anthropometry.App.Tests --configuration Release
dotnet build src/Anthropometry.App/Anthropometry.App.csproj -f net10.0-android -c Release
```

Expected: ViewModel tests pass and the Android app builds.

- [ ] **Step 7: Commit the profile UI slice**

```powershell
git add src/Anthropometry.App tests/Anthropometry.App.Tests
git commit -m "feat: add profile management screens"
```

---

### Slice 6: Measurement entry and calculation results

**Review boundary:** A user can enter a valid measurement for a selected profile and view body-fat, BMR, and TDEE results stored for that measurement.

**Files:**

- Create: `src/Anthropometry.App/Features/Measurements/MeasurementEditorPage.xaml`
- Create: `src/Anthropometry.App/Features/Measurements/MeasurementEditorPage.xaml.cs`
- Create: `src/Anthropometry.App/Features/Measurements/MeasurementEditorViewModel.cs`
- Create: `src/Anthropometry.App/Features/Results/CalculationResultPage.xaml`
- Create: `src/Anthropometry.App/Features/Results/CalculationResultPage.xaml.cs`
- Create: `src/Anthropometry.App/Features/Results/CalculationResultViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileListPage.xaml`
- Modify: `src/Anthropometry.App/AppShell.xaml`
- Test: `tests/Anthropometry.App.Tests/Features/Measurements/MeasurementEditorViewModelTests.cs`
- Test: `tests/Anthropometry.App.Tests/Features/Results/CalculationResultViewModelTests.cs`

**Interfaces:**

- The editor consumes `RecordMeasurement` and the three calculation use cases.
- The result ViewModel consumes application DTOs containing value, unit, formula identity, and formula version.
- No page or ViewModel references a formula class directly.

- [ ] **Step 1: Write failing measurement-entry tests**

Cover metric numeric parsing, required fields, invalid ranges, activity-level selection, disabled save while invalid, and successful submission.

- [ ] **Step 2: Implement the measurement editor**

Use explicit labels and units (`kg`, `cm`, `years`). Keep all entered values as text until validation converts them to typed Application input.

- [ ] **Step 3: Write failing result tests**

Verify loading, all three result cards, units, formula version visibility in a details area, and recoverable calculation errors.

- [ ] **Step 4: Implement result orchestration and display**

After a measurement is saved, calculate the three results through Application and navigate to the result page. Persist each result through the calculation use case, not from the page.

- [ ] **Step 5: Add Android keyboard and scrolling behavior**

Verify that the form scrolls when the keyboard is visible, fields retain focus correctly, and the primary action remains reachable on small screens.

- [ ] **Step 6: Run tests and build**

```powershell
dotnet test --configuration Release
dotnet build src/Anthropometry.App/Anthropometry.App.csproj -f net10.0-android -c Release
```

Expected: all tests pass and the Android app builds.

- [ ] **Step 7: Commit the measurement and result slice**

```powershell
git add src/Anthropometry.App tests/Anthropometry.App.Tests
git commit -m "feat: add measurement entry and calculation results"
```

---

### Slice 7: History, error states, and responsive hardening

**Review boundary:** The app communicates persistence and calculation failures clearly, shows measurement history, and behaves consistently across supported Android screen sizes.

**Files:**

- Create: `src/Anthropometry.App/Features/Measurements/MeasurementHistoryPage.xaml`
- Create: `src/Anthropometry.App/Features/Measurements/MeasurementHistoryPage.xaml.cs`
- Create: `src/Anthropometry.App/Features/Measurements/MeasurementHistoryViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileDetailPage.xaml`
- Modify: `src/Anthropometry.App/Resources/Styles/Styles.xaml`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.resx`
- Test: `tests/Anthropometry.App.Tests/Features/Measurements/MeasurementHistoryViewModelTests.cs`
- Test: `tests/Anthropometry.Infrastructure.Tests/Persistence/MigrationUpgradeTests.cs`
- Test: `tests/Anthropometry.App.Tests/ResponsiveLayoutTests.cs`

**Interfaces:**

- History consumes `GetMeasurementHistory` and displays newest measurements first.
- Selecting a history item opens its persisted calculation results.
- All feature ViewModels expose loading, empty, success, and recoverable-error states.

- [ ] **Step 1: Write history and error-state tests**

Cover empty history, multiple measurements sorted newest first, missing result data, SQLite failure translated to a user-safe error, and retry behavior.

- [ ] **Step 2: Implement history UI**

Show date, key measurements, and available result summary without duplicating calculation logic.

- [ ] **Step 3: Add localized strings and consistent error components**

Centralize user-facing strings and reuse a small error-state component with a retry action.

- [ ] **Step 4: Add migration upgrade coverage**

Create a database at schema version 1, apply the next migration fixture, and verify existing profiles, measurements, and results remain readable.

- [ ] **Step 5: Test responsive layouts**

Verify the profile list, measurement form, results, and history at the smallest supported phone width and a larger Android width. Check larger font settings and keyboard-visible form states.

- [ ] **Step 6: Run all tests and Android build**

```powershell
dotnet test --configuration Release
dotnet build src/Anthropometry.App/Anthropometry.App.csproj -f net10.0-android -c Release
```

Expected: all tests pass and the Android app builds without warnings introduced by this slice.

- [ ] **Step 7: Commit the history and hardening slice**

```powershell
git add src/Anthropometry.App src/Anthropometry.Infrastructure tests
git commit -m "feat: add measurement history and resilient ui states"
```

---

### Post-MVP feature slice: Profile settings and measurement modes

**Status:** Implemented on `master` in the approved inline execution.

**Review boundary:** Profiles persist the settings needed for future measurements, users can choose between weight-only and extended entries, and weight-only history entries can show recalculated results using the latest earlier size-based measurement.

**Acceptance criteria:**

- [x] Profiles persist height, age, and activity level locally, and profile edits update these settings.
- [x] New profiles are limited to four by the Application use case.
- [x] Zero profiles show only the centered create action; existing profiles show full-width bottom `Add profile`.
- [x] At four profiles, `Add profile` remains visible and is disabled.
- [x] `Add weight` is disabled until a size-based measurement exists; it stores only the new weight and recalculates results from the latest earlier neck and abdomen sizes.
- [x] `Add measurements` stores weight plus neck and abdomen and runs the existing three calculations.
- [x] Both measurement save flows open the refreshed History screen; results remain available for both measurement types.
- [x] Measurement records retain the profile-setting snapshot used at capture time.
- [x] The profile detail screen does not show a warning icon; weight-only history rows show a centered warning icon before the date with a yellow shadow.
- [x] History uses friendly measurement labels and `dd/MM/yyyy` dates, and both measurement types expose `View results`.
- [x] Visible profile actions and labels use friendly copy; toolbar Settings and Help actions use accessible gear and question-mark icons.

**Implementation records:**

- Design: `docs/superpowers/specs/2026-09-09-profile-measurement-ux-design.md`
- Execution plan: `docs/superpowers/plans/2026-09-09-profile-measurement-ux.md`
- Manual checklist: `docs/testing/profile-measurement-acceptance.md`

**Automated verification completed on 2026-09-09:**

- [x] `dotnet restore -m:1`
- [x] `dotnet build --configuration Release -m:1`
- [x] `dotnet test --configuration Release -m:1` — 93 tests passed across Domain, Application, Infrastructure, and App projects.
- [x] `dotnet build src/Anthropometry.App/Anthropometry.App.csproj -f net10.0-android -c Release -m:1` — Android target built with 0 warnings and 0 errors.
- [ ] Manual Android acceptance checklist — pending a device/emulator pass.

---

### Post-MVP feature slice: Settings and persisted localization

**Status:** Implemented on `master` in the approved inline execution.

**Review boundary:** The app provides Presentation-only Settings and Help screens, icon-only toolbar actions, and persisted English, Spanish, and German translations without adding a database migration or external dependency.

**Acceptance criteria:**

- [x] Settings opens from the gear toolbar icon and Help remains available from the question-mark toolbar icon.
- [x] Help opens a localized page with the health disclaimer and the body-fat, BMR, and TDEE equations used by the app.
- [x] The Settings screen offers exactly English, Spanish, and German.
- [x] Selecting a language applies translated visible copy immediately and persists the language code locally.
- [x] App startup restores the persisted language before the first feature page is created; English is the fallback when no valid preference exists.
- [x] Existing user-facing labels, buttons, validation messages, activity levels, history labels, and result titles use localized resources.
- [x] Localization remains in Presentation; Domain and Application stay independent of MAUI and resource storage.
- [x] Views do not repeat their navigation title in page content; distinct headings such as `Estimated results` remain.

**Implementation records:**

- Design: `docs/superpowers/specs/2026-09-10-settings-localization-design.md`
- Execution plan: `docs/superpowers/plans/2026-09-10-settings-localization.md`

**Verification on 2026-09-10:**

- [x] `dotnet test Anthropometry.sln -f net10.0 --configuration Release -m:1` — 107 tests passed.
- [x] `dotnet build src/Anthropometry.App/Anthropometry.App.csproj -f net10.0-android -c Debug -m:1` — 0 warnings and 0 errors.
- [x] Emulator smoke check with an isolated package — Settings opened, Spanish applied immediately, and Spanish was restored after a cold restart.

**Help-page verification on 2026-09-11:**

- [x] `dotnet test --configuration Release` — 125 tests passed across Domain, Application, Infrastructure, and App projects.
- [x] `dotnet build src/Anthropometry.App/Anthropometry.App.csproj --configuration Release --framework net10.0-android` — 0 warnings and 0 errors.
- [x] Emulator smoke check — Help opened from the question-mark icon and displayed the localized disclaimer, body-fat, BMR, and TDEE equations.

---

### Post-MVP feature slice: Weight graphic

**Status:** Implemented on `master` as an offline Presentation feature.

**Review boundary:** History provides a ruler toolbar action that opens a `Weight graphic` option. The resulting page reuses the existing measurement-history use case and renders every persisted weight as a chronological point connected by a line, without adding a chart package or database changes.

**Acceptance criteria:**

- [x] History exposes an accessible ruler icon and a localized `Weight graphic` menu option.
- [x] The weight graphic page loads all measurements through `GetMeasurementHistory`.
- [x] Measurements are ordered oldest to newest on the X axis, with weight values on the Y axis.
- [x] Every measurement is represented by a point and points are joined by a line.
- [x] The chart uses compact point spacing, remains horizontally scrollable for larger histories, pads the Y axis by 10 kg below the minimum and above the maximum, and shows localized axis labels.
- [x] Tapping near a point shows its localized date and weight legend; tapping elsewhere in the chart clears the legend.
- [x] Empty and recoverable-error states are available and localized in English, Spanish, and German.

**Verification:**

- [x] App tests cover chronological point mapping, compact date labels, empty state, load failure translation, and the History chart command.
- [x] Android Debug build completed with 0 warnings and 0 errors.

---

### Post-MVP feature slice: Profile gender

**Status:** Implemented on `master` as a local profile-data and Presentation feature; gender-specific equations are implemented in the following slice.

**Review boundary:** Profile creation and editing allow a required Male/Female selection, the value is validated and persisted in SQLite through schema migration 3, existing profiles default to Male, and the profile list/detail title show the corresponding gender symbol.

**Acceptance criteria:**

- [x] The Domain profile model accepts only `Male` or `Female` and preserves the selected value through create, update, and rehydration.
- [x] The Application profile use cases carry gender through commands and `ProfileDto` without changing formula behavior.
- [x] SQLite schema version 3 stores profile gender and migrates existing rows to `Male`.
- [x] The profile editor shows localized Male/Female options and persists the selected value.
- [x] Profile rows show a gender icon before the name, and the profile detail navigation title includes the same icon.
- [x] Gender labels and validation copy are available in English, Spanish, and German.

**Verification:**

- [x] Domain, Application, Infrastructure, and App tests cover gender validation, persistence, migration defaults, editor state, icon mapping, and markup.
- [x] The Android Release build completed with 0 warnings and 0 errors.

---

### Post-MVP feature slice: Gender-specific calculations

**Status:** Implemented on `master` as the approved female-calculation extension.

**Review boundary:** Female profiles use the classic versioned US Navy/Hodgdon-Beckett body-fat equation with waist, hip, neck, and height; female profiles use the female Mifflin-St Jeor BMR equation; TDEE reuses the existing activity multiplier; measurement gender and hip values are persisted for correct historical and weight-only recalculations.

**Acceptance criteria:**

- [x] Domain tests cover the female body-fat equation, metric-to-imperial conversion, invalid logarithm inputs, formula identity, version, and units.
- [x] Domain tests cover the female Mifflin-St Jeor equation, invalid inputs, formula identity, version, and units.
- [x] Female `WeightAndSizes` measurements require and persist hip circumference; `WeightOnly` measurements continue to reject all size fields.
- [x] Measurement records persist the selected gender and migration 4 defaults legacy records to Male.
- [x] Calculation use cases select male or female body-fat and BMR formulas from the measurement gender snapshot.
- [x] Female TDEE uses female BMR multiplied by the existing named activity factor.
- [x] Female weight-only calculations reuse the latest earlier female size measurement, including hip circumference.
- [x] Female measurement entry shows waist and hip labels and validates decimal metric inputs.
- [x] Help displays both male and female body-fat and BMR equations in English, Spanish, and German.
- [x] Female sample-history generation persists hip variations and female calculation results.

**Verification:**

- [x] `dotnet test Anthropometry.sln -f net10.0 --configuration Release --no-restore -m:1` — 159 tests passed.
- [ ] Android Release build and manual female-profile acceptance pass — pending the final verification command/device pass.

---

### Post-MVP feature slice: Persisted appearance themes

**Status:** Implemented on `master` as a Presentation-only appearance preference.

**Review boundary:** Settings allows users to switch between the default Light theme and a neutral Dark theme. The selection is stored in local Preferences, restored before the first feature page is created, and applied immediately while the app is open.

**Acceptance criteria:**

- [x] The default theme is Light and the Settings page exposes localized Light and Dark options.
- [x] Selecting a theme applies it immediately and persists the canonical theme code locally.
- [x] App startup restores the saved theme before creating the first feature page.
- [x] The Dark theme uses neutral near-black backgrounds, charcoal controls, slate borders, and high-contrast text across shared styles and the weight chart.
- [x] Toolbar icons use distinct light and dark assets with valid Android resource names.
- [x] Default `.NET` startup artwork remains absent from the splash and launcher assets.

**Verification:**

- [x] App tests cover default and restored themes, persistence notifications, Settings selection, dark resource bindings, and theme-specific icon assets.
- [x] Android Release build completed with 0 warnings and 0 errors.

---

### Post-MVP feature slice: Persisted display preferences and measurement system

**Status:** Implemented as a Presentation-only preference slice.

**Review boundary:** Settings persists the date order (`dd/mm/yyyy` or `mm/dd/yyyy`) and one Metric/Imperial measurement-system choice. Metric displays profile height in meters, circumferences in centimeters, and weight in kilograms. Imperial displays profile height in decimal feet, circumferences in inches, and weight in pounds. Profile and measurement inputs convert at the Presentation boundary; Domain calculations and SQLite remain in kilograms, centimeters, and UTC. History and the weight graphic use the selected date and derived units.

**Acceptance criteria:**

- [x] Display preferences default to `dd/mm/yyyy` and Metric (meters/centimeters/kilograms) and restore from local Preferences.
- [x] Settings exposes localized date-format and Metric/Imperial selectors in English, Spanish, and German.
- [x] Profile height input converts meters or decimal feet to canonical centimeters; measurement weight converts kilograms or pounds, and circumference inputs convert centimeters or inches before Application use cases run.
- [x] History formats local dates with the selected order and displays weight/height in the selected units.
- [x] The weight graphic keeps canonical kilogram points internally but displays selected-unit values, padding, legends, and date labels.
- [x] Formula inputs, calculation results, SQLite schema, and persisted timestamps remain unchanged.
- [x] App tests cover preference persistence/defaults, conversions, settings bindings, input boundaries, history formatting, and chart formatting.

**Verification:**

- [x] App test suite passed with 96 tests.
- [x] Full solution test suite passed: 52 Domain, 31 Application, 11 Infrastructure, and 96 App tests.
- [x] Android Debug build completed with 0 warnings and 0 errors.
- [x] Android Release source/package build completed with 0 warnings and 0 errors when `PublishTrimmed=false` and `RunAOTCompilation=false` were supplied as environment-only verification overrides.
- [ ] Default trimmed Android Release packaging remains blocked by the host's `Microsoft.NET.ILLink` task-host failure (`MSB4216`), unrelated to application compilation.

---

### Post-MVP feature slice: Profile CSV transfer

**Status:** Implemented on `master` as an offline, Android-first profile transfer feature. Manual Android Sharesheet/FilePicker smoke validation remains pending.

**Review boundary:** A user can export one complete profile to a versioned canonical CSV file, share it through Android, import it as a new profile after preview and confirmation, and retain its measurements and historical calculation results without recalculation or external identifiers.

**Acceptance criteria:**

- [x] Export contains one profile, canonical kg/cm values, UTC timestamps, measurements, and historical results in a fixed v1 CSV schema.
- [x] CSV values use invariant decimals, UTF-8, CSV escaping, and version validation; malformed, duplicate, or dangling records are rejected before persistence.
- [x] Import always generates new profile, measurement, and calculation-result IDs and preserves gender, hip values, weight-only history, formula IDs, versions, units, values, and timestamps.
- [x] Import enforces the four-profile limit and persists the complete graph in one SQLite transaction with rollback on failure.
- [x] Android uses the private cache directory, native Sharesheet, and FilePicker/Storage Access Framework without storage permissions or new packages.
- [x] Profile detail exposes localized `Export profile`; profile list exposes localized `Import profile`, disabled at four profiles and reloaded after a confirmed import.
- [x] English, Spanish, and German resources cover labels, health-data warning, preview confirmation, success, cancellation, invalid-file, unsupported-format, and profile-limit states.

**Automated verification:**

- [x] Serializer tests: 7 passed.
- [x] Application tests: 55 passed.
- [x] Infrastructure tests: 16 passed.
- [x] App tests: 144 passed.
- [x] Full Release verification with `PublishTrimmed=false` and `RunAOTCompilation=false`: Domain 54, Application 55, Infrastructure 16, and App 144 tests passed.
- [x] Android Debug build with `PublishTrimmed=false` and `RunAOTCompilation=false`: 0 warnings, 0 errors.
- [x] Android Release build with `PublishTrimmed=false` and `RunAOTCompilation=false`: 0 warnings, 0 errors.
- [ ] Android emulator/Pixel Sharesheet, import-provider, cancellation, and four-profile smoke pass — pending device execution.

The default trimmed Release verification remains blocked by the installed MAUI linker task host (`MSB4216`/`MSB4027` while creating the .NET x64 task host). The source and test verification above uses the documented overrides; production trimming/AOT packaging still needs a host/toolchain follow-up.

**Implementation records:**

- Design: `docs/superpowers/specs/2026-09-19-profile-csv-transfer-design.md`
- Execution plan: `docs/superpowers/plans/2026-09-19-profile-csv-transfer-plan.md`

---

### Slice 8: Release verification and handoff

**Review boundary:** The MVP is reproducibly buildable, testable, privacy-reviewed, and ready for a manual Android acceptance pass.

**Files:**

- Create: `.editorconfig`
- Create: `README.md`
- Create: `docs/testing/android-acceptance.md`
- Create: `docs/testing/profile-measurement-acceptance.md`
- Modify: `ARCHITECTURE.md` if an implementation decision changed
- Modify: `AGENTS.md` if agent workflow or constraints changed
- Modify: `PLAN.md` to mark completed slices and record verified commands

- [ ] **Step 1: Add repository formatting and analyzer configuration**

Configure nullable reference types, implicit usings, warnings as errors for production projects where the existing SDK supports it, and consistent formatting.

- [ ] **Step 2: Document local setup**

Document the required .NET 10 SDK, Android SDK, JDK, emulator/device setup, restore command, build command, test command, and how to choose a debug Android target.

- [ ] **Step 3: Run full verification**

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
dotnet build src/Anthropometry.App/Anthropometry.App.csproj -f net10.0-android -c Release
```

Expected: every command succeeds from a clean checkout.

- [ ] **Step 4: Perform Android acceptance testing**

Verify:

1. the app starts without a network connection;
2. a profile can be created, renamed, and deleted;
3. invalid measurement values are rejected with actionable messages;
4. a valid measurement produces body-fat, BMR, and TDEE results;
5. closing and reopening the app preserves profiles and history;
6. profile deletion removes its measurements and results after confirmation;
7. loading, empty, success, and recoverable-error states are understandable;
8. the UI remains usable on small and large Android screens.

- [ ] **Step 5: Review privacy and dependency surface**

Confirm that no unnecessary permissions, network calls, analytics packages, credentials, personal sample data, or unused dependencies were introduced.

- [ ] **Step 6: Commit the release-verification slice**

```powershell
git add .editorconfig README.md docs ARCHITECTURE.md AGENTS.md PLAN.md
git commit -m "chore: document and verify android mvp"
```

## Plan self-review checklist

- [ ] Every architecture requirement has at least one slice.
- [ ] Every slice has a review boundary and test checkpoint.
- [ ] Formula identity, version, units, and historical persistence are covered.
- [ ] SQLite schema, migration, repository, and transaction behavior are covered.
- [ ] The UI never bypasses Application use cases.
- [ ] Android-first behavior and later iOS reuse remain explicit.
- [ ] No task depends on an unspecified backend, account, or network service.
- [ ] All project Markdown deliverables are written in English.
