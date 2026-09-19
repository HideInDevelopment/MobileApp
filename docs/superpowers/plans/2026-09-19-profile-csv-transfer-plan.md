# Profile CSV Transfer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add local profile export/import using a versioned CSV file that preserves one profile, its measurements, and historical calculation results while integrating with Android sharing and file picking.

**Architecture:** The Application layer owns the transfer contract, CSV serialization, validation, ID remapping, and use cases. Infrastructure adds one focused transactional transfer port over the existing SQLite tables. Presentation adds profile-list/detail commands and a thin MAUI adapter for `FilePicker` and `Share`; no domain or database schema changes are introduced.

**Tech Stack:** .NET 10, .NET MAUI/XAML, existing SQLite infrastructure, `CommunityToolkit.Mvvm`, xUnit, Android Sharesheet, Android Storage Access Framework.

**Spec:** `docs/superpowers/specs/2026-09-19-profile-csv-transfer-design.md`

## Global Constraints

- Export exactly one profile per file and always import as a new profile.
- Persist and calculate imported values in kilograms, centimeters, and UTC; never serialize display units or localized decimal/date formats.
- Preserve imported calculation values, units, formula IDs, and formula versions; do not recalculate historical results during import.
- Generate new IDs for the imported profile, measurements, and calculation results; use source IDs only for validating and remapping relationships.
- Enforce the existing four-profile limit before writing any imported data.
- Import the complete profile graph in one SQLite transaction and leave the database unchanged on failure.
- Use `FileSystem.CacheDirectory`, `ShareFileRequest`, and `FilePicker`; do not add storage permissions or a package dependency.
- Keep all user-facing copy localizable in English, Spanish, and German.

## Review Focus

- CSV fields containing commas, quotes, or embedded newlines must round-trip without changing names or text values; test this in the serializer task.
- A valid file with duplicate source IDs or a result referencing an unknown measurement must be rejected before persistence; test this in the serializer/application task.
- A file with a female measurement must retain waist/abdomen and hip values, gender, and all historical results; test this in the application round-trip task.
- A failed SQLite insert must roll back the profile and every previously inserted child row; test this in the infrastructure transaction task.
- Import at the four-profile limit and user cancellation must leave the profile list and database unchanged; test this in the application/UI task.

---

### Task 1: Define the transfer document and CSV serializer

**Files:**
- Create: `src/Anthropometry.Application/Profiles/ProfileTransferModels.cs`
- Create: `src/Anthropometry.Application/Profiles/ProfileTransferCsvSerializer.cs`
- Create: `tests/Anthropometry.Application.Tests/Profiles/ProfileTransferCsvSerializerTests.cs`

**Interfaces:**
- Produce these records in the `Anthropometry.Application.Profiles` namespace:

  ```csharp
  public sealed record ProfileTransferSnapshot(
      Profile Profile,
      IReadOnlyList<Measurement> Measurements,
      IReadOnlyList<CalculationResult> Results);

  public sealed record ProfileTransferDocument(
      int FormatVersion,
      DateTimeOffset ExportedAtUtc,
      ProfileTransferProfile Profile,
      IReadOnlyList<ProfileTransferMeasurement> Measurements,
      IReadOnlyList<ProfileTransferCalculationResult> Results);

  public sealed record ProfileTransferProfile(
      Guid SourceProfileId,
      string Name,
      ProfileGender Gender,
      decimal? HeightCm,
      int? AgeYears,
      ActivityLevel? ActivityLevel,
      DateTimeOffset CreatedAtUtc,
      DateTimeOffset UpdatedAtUtc);

  public sealed record ProfileTransferMeasurement(
      Guid SourceProfileId,
      Guid SourceMeasurementId,
      MeasurementType Type,
      DateTimeOffset MeasuredAtUtc,
      decimal WeightKg,
      decimal HeightCm,
      decimal? NeckCm,
      decimal? AbdomenCm,
      decimal? HipCm,
      ProfileGender Gender,
      int AgeYears,
      ActivityLevel ActivityLevel);

  public sealed record ProfileTransferCalculationResult(
      Guid SourceResultId,
      Guid SourceMeasurementId,
      CalculationType CalculationType,
      decimal Value,
      string Unit,
      string FormulaId,
      string FormulaVersion,
      DateTimeOffset CalculatedAtUtc);

  public sealed record ProfileExportFile(string FileName, byte[] Content);

  public sealed record ProfileImportPreview(
      ProfileTransferDocument Document,
      string Name,
      ProfileGender Gender,
      int MeasurementCount,
      int CalculationResultCount);

  public sealed record ImportedProfile(
      ProfileDto Profile,
      int MeasurementCount,
      int CalculationResultCount);
  ```
- Implement `ProfileTransferCsvSerializer.Serialize(ProfileTransferSnapshot snapshot) -> byte[]`.
- Implement `ProfileTransferCsvSerializer.Parse(Stream content) -> Result<ProfileTransferDocument>`.

- [ ] **Step 1: Write failing serializer tests**

  Add tests that build a female profile with a size measurement, a weight-only measurement, and three calculation results, then assert that serialization and parsing preserve profile settings, canonical decimals, UTC timestamps, measurement relationships, hip values, formula IDs, versions, units, and values. Add a name containing a comma, quote, and newline to exercise CSV escaping. Add tests for invariant decimal output, UTF-8 BOM input, duplicate source IDs, dangling result references, malformed required fields, and unsupported `format_version`.

- [ ] **Step 2: Run the focused tests and confirm the expected red state**

  Run:

  ```powershell
  dotnet test .\tests\Anthropometry.Application.Tests\Anthropometry.Application.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~ProfileTransferCsvSerializerTests"
  ```

  Expected: compilation or assertion failures because the transfer models and serializer do not exist yet.

- [ ] **Step 3: Implement the minimum transfer contract and serializer**

  Use the fixed v1 column order from the spec. Emit `meta`, `profile`, `measurement`, and `calculation_result` records. Format decimals with `CultureInfo.InvariantCulture`, timestamps with UTC round-trip format, nullable values as empty fields, and all CSV fields through one quoting helper. Parse into typed records, validate the single-profile shape, source-ID uniqueness, measurement/result references, supported enum values, and domain constraints before returning a document.

- [ ] **Step 4: Run serializer tests and refactor only for clarity**

  Re-run the focused command. Expected: all serializer tests pass with no new package or platform dependency. Keep CSV parsing/writing in this focused Application file; do not add a generic serialization framework.

- [ ] **Step 5: Commit the serializer slice**

  ```powershell
  git add src/Anthropometry.Application/Profiles/ProfileTransferModels.cs src/Anthropometry.Application/Profiles/ProfileTransferCsvSerializer.cs tests/Anthropometry.Application.Tests/Profiles/ProfileTransferCsvSerializerTests.cs
  git commit -m "feat: add versioned profile transfer csv"
  ```

### Task 2: Add the transactional transfer port and SQLite implementation

**Files:**
- Create: `src/Anthropometry.Application/Abstractions/IProfileTransferRepository.cs`
- Create: `src/Anthropometry.Infrastructure/Repositories/SqliteProfileTransferRepository.cs`
- Create: `tests/Anthropometry.Infrastructure.Tests/Repositories/SqliteProfileTransferRepositoryTests.cs`
- Modify: `src/Anthropometry.App/MauiProgram.cs`

**Interfaces:**
- Add `IProfileTransferRepository.GetSnapshotAsync(ProfileId profileId, CancellationToken cancellationToken) -> Task<ProfileTransferSnapshot?>`.
- Add `IProfileTransferRepository.ImportAsync(Profile profile, IReadOnlyList<Measurement> measurements, IReadOnlyList<CalculationResult> results, CancellationToken cancellationToken) -> Task`.
- Register `SqliteProfileTransferRepository` as the Application port implementation in `MauiProgram.cs` only after its tests pass.

- [ ] **Step 1: Write failing SQLite tests**

  Using the existing temporary SQLite test setup, assert that `GetSnapshotAsync` returns one profile, all of its measurements, and all calculation results. Assert that `ImportAsync` inserts the complete graph and that a deliberate duplicate child ID causes the transaction to roll back the profile and every child row.

- [ ] **Step 2: Run the focused infrastructure tests and verify the failure**

  ```powershell
  dotnet test .\tests\Anthropometry.Infrastructure.Tests\Anthropometry.Infrastructure.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~SqliteProfileTransferRepositoryTests"
  ```

  Expected: the new repository type and port are missing.

- [ ] **Step 3: Implement the focused SQLite transfer repository**

  Open one connection from `SqliteConnectionFactory`. For snapshots, query the profile, its measurements, and all results in that profile. For imports, call `RunInTransaction`, insert the profile row, measurement rows, and result rows in dependency order, and honor cancellation before entering the transaction. Reuse the existing UTC converter and the current table columns; do not add a migration.

- [ ] **Step 4: Run the focused infrastructure tests**

  Expected: snapshot, complete import, and rollback tests pass. Existing repository and migration tests must remain green.

- [ ] **Step 5: Commit the persistence slice**

  ```powershell
  git add src/Anthropometry.Application/Abstractions/IProfileTransferRepository.cs src/Anthropometry.Infrastructure/Repositories/SqliteProfileTransferRepository.cs tests/Anthropometry.Infrastructure.Tests/Repositories/SqliteProfileTransferRepositoryTests.cs
  git commit -m "feat: add transactional profile transfer persistence"
  ```

### Task 3: Implement export and import application use cases

**Files:**
- Create: `src/Anthropometry.Application/Profiles/ExportProfile.cs`
- Create: `src/Anthropometry.Application/Profiles/ImportProfile.cs`
- Modify: `src/Anthropometry.Application/Common/ApplicationErrors.cs`
- Create: `tests/Anthropometry.Application.Tests/Profiles/ProfileTransferUseCaseTests.cs`
- Modify: `tests/Anthropometry.Application.Tests/Support/Fakes.cs`

**Interfaces:**
- Add `ExportProfileCommand(ProfileId ProfileId)`.
- Add `ExportProfile.ExecuteAsync(ExportProfileCommand command, CancellationToken cancellationToken) -> Task<Result<ProfileExportFile>>`.
- Add `ImportProfile.PreviewAsync(Stream content, CancellationToken cancellationToken) -> Task<Result<ProfileImportPreview>>`.
- Add `ImportProfile.ExecuteAsync(ProfileImportPreview preview, CancellationToken cancellationToken) -> Task<Result<ImportedProfile>>`.
- `ProfileImportPreview` must expose only the preview counts/name plus the validated `ProfileTransferDocument` needed for the confirmed import.

- [ ] **Step 1: Write failing application tests**

  Add tests for export of a missing profile, successful export filename/content, import preview counts, successful import with regenerated IDs, preservation of female/weight-only fields and formula metadata, the four-profile limit, malformed/unsupported input errors, and a failed repository import that returns a persistence error without reporting success.

- [ ] **Step 2: Run the focused tests and verify the expected failures**

  ```powershell
  dotnet test .\tests\Anthropometry.Application.Tests\Anthropometry.Application.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~ProfileTransferUseCaseTests"
  ```

- [ ] **Step 3: Implement export**

  `ExportProfile` loads the complete snapshot through `IProfileTransferRepository`, serializes it, and returns a sanitized filename such as `anthropometry-<profile-name>-<yyyyMMdd>.csv` plus UTF-8 content. It must never log raw profile, measurement, or result data.

- [ ] **Step 4: Implement preview and import**

  `PreviewAsync` parses the stream through `ProfileTransferCsvSerializer` and returns the profile name, gender, measurement count, and result count. `ExecuteAsync` checks the current profile count, creates a new profile with `Profile.Rehydrate(ProfileId.New(), ...)`, remaps every source measurement ID to a new `MeasurementId`, remaps result references, validates each entity through `Measurement.Rehydrate` and `CalculationResult.Create`, and calls the transactional repository once. It must preserve source timestamps and result formula metadata while never reusing source IDs.

- [ ] **Step 5: Run the focused tests and the existing Application suite**

  ```powershell
  dotnet test .\tests\Anthropometry.Application.Tests\Anthropometry.Application.Tests.csproj --configuration Debug --no-restore
  ```

  Expected: all existing Application tests plus the new transfer tests pass.

- [ ] **Step 6: Commit the application slice**

  ```powershell
  git add src/Anthropometry.Application/Profiles/ExportProfile.cs src/Anthropometry.Application/Profiles/ImportProfile.cs src/Anthropometry.Application/Common/ApplicationErrors.cs tests/Anthropometry.Application.Tests/Profiles/ProfileTransferUseCaseTests.cs
  git commit -m "feat: add profile transfer use cases"
  ```

### Task 4: Add the MAUI file-sharing and file-picking adapter

**Files:**
- Create: `src/Anthropometry.App/Features/Profiles/IProfileTransferFileService.cs`
- Create: `src/Anthropometry.App/Features/Profiles/MauiProfileTransferFileService.cs`
- Modify: `src/Anthropometry.App/MauiProgram.cs`

**Interfaces:**
- Add `IProfileTransferFileService.ShareAsync(ProfileExportFile file, string title, CancellationToken cancellationToken) -> Task`.
- Add `IProfileTransferFileService.PickCsvAsync(string title, CancellationToken cancellationToken) -> Task<Stream?>`.

- [ ] **Step 1: Implement the thin platform adapter**

  Write exports to `FileSystem.CacheDirectory`, share them with `Share.Default.RequestAsync(new ShareFileRequest { Title = title, File = new ShareFile(path) })`, and use `FilePicker.Default.PickAsync` with Android file types `text/csv` and `.csv`. Copy the selected `FileResult` stream into a `MemoryStream`, reset its position, and return it; return `null` on cancellation. Do not request storage permissions. Clean old app-created transfer files when creating a new export, not immediately after sharing.

- [ ] **Step 2: Register the adapter and run the Android compile check**

  Register the adapter as a singleton and run:

  ```powershell
  dotnet build .\src\Anthropometry.App\Anthropometry.App.csproj -f net10.0-android -c Debug -m:1 -p:PublishTrimmed=false -p:RunAOTCompilation=false -p:AndroidSdkDirectory="$env:LOCALAPPDATA\Android\Sdk" -p:JavaSdkDirectory="C:\Program Files\Microsoft\jdk-21.0.12.101-hotspot" --verbosity minimal
  ```

  Expected: 0 warnings and 0 errors.

- [ ] **Step 3: Commit the platform adapter**

  ```powershell
  git add src/Anthropometry.App/Features/Profiles/IProfileTransferFileService.cs src/Anthropometry.App/Features/Profiles/MauiProfileTransferFileService.cs src/Anthropometry.App/MauiProgram.cs
  git commit -m "feat: add android profile transfer file adapter"
  ```

### Task 5: Add profile-list/detail commands and localized UI

**Files:**
- Modify: `src/Anthropometry.App/Features/Profiles/IProfileNavigation.cs`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileDetailViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileDetailPage.xaml`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileListViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Profiles/ProfileListPage.xaml`
- Modify: `src/Anthropometry.App/MauiNavigation.cs`
- Modify: `src/Anthropometry.App/Localization/LanguageService.cs`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.resx`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.es.resx`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.de.resx`
- Modify: `tests/Anthropometry.App.Tests/Features/Profiles/ProfileDetailViewModelTests.cs`
- Modify: `tests/Anthropometry.App.Tests/Features/Profiles/ProfileListViewModelTests.cs`
- Modify: `tests/Anthropometry.App.Tests/Features/Profiles/ProfileEditorViewModelTests.cs`
- Modify: `tests/Anthropometry.App.Tests/ResponsiveLayoutTests.cs`
- Create: `tests/Anthropometry.App.Tests/Features/Profiles/ProfileTransferMarkupTests.cs`

**Interfaces:**
- Extend `IProfileNavigation` with `Task ExportProfileAsync(ProfileDto profile)` and `Task ImportProfileAsync()`.
- Add `ProfileDetailViewModel.ExportCommand`.
- Add `ProfileListViewModel.ImportCommand` and `CanImportProfile`, with import disabled when four profiles already exist.

- [ ] **Step 1: Write failing ViewModel and markup tests**

  Add navigation-spy tests that execute the detail export command and list import command, assert import availability below four profiles and disabled state at four, and assert the XAML binds `ExportProfile` and `ImportProfile` to the correct commands. Add tests that a successful import reloads the profile list and that a cancelled import does not clear or alter the current list.

- [ ] **Step 2: Run focused App tests and verify the expected failures**

  ```powershell
  dotnet test .\tests\Anthropometry.App.Tests\Anthropometry.App.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~ProfileDetailViewModelTests|FullyQualifiedName~ProfileListViewModelTests|FullyQualifiedName~ProfileTransferMarkupTests"
  ```

- [ ] **Step 3: Add the commands and UI**

  Add a localized `Export profile` button to profile detail. Add a localized `Import profile` button to the profile list alongside the existing profile actions, keeping it usable when the list is empty and disabled at the four-profile limit. Keep loading, error, cancellation, and success feedback localized and avoid putting CSV rules in XAML.

- [ ] **Step 4: Implement navigation orchestration**

  Resolve `ExportProfile`, `ImportProfile`, and `IProfileTransferFileService` in `MauiNavigation`. Export should call the use case, show the health-data warning, and then share the returned file. Import should pick a CSV, preview it, show the confirmation summary, execute only after confirmation, translate controlled errors, and return without mutation on cancel. `ProfileListViewModel` reloads after `ImportProfileAsync` returns.

- [ ] **Step 5: Add English, Spanish, and German resources and run focused tests**

  Add resource keys for export/import labels, preview counts, confirmation, privacy warning, success, cancellation, unsupported format, invalid file, and profile-limit errors. Run the focused App command and markup tests and then the complete App test project.

- [ ] **Step 6: Commit the Presentation slice**

  ```powershell
  git add src/Anthropometry.App/Features/Profiles src/Anthropometry.App/MauiNavigation.cs src/Anthropometry.App/Localization/LanguageService.cs src/Anthropometry.App/Resources/Strings tests/Anthropometry.App.Tests/Features/Profiles
  git commit -m "feat: add profile export and import actions"
  ```

### Task 6: Update documentation, perform full verification, and smoke-test Android

**Files:**
- Modify: `ARCHITECTURE.md`
- Modify: `PLAN.md`
- Modify: `docs/superpowers/specs/2026-09-19-profile-csv-transfer-design.md` only if implementation decisions changed during review
- Modify: `docs/superpowers/plans/2026-09-19-profile-csv-transfer-plan.md` to mark completed steps during execution

- [ ] **Step 1: Update architecture and the active plan**

  Replace the planned generic `CSV or PDF export` note with the implemented v1 profile CSV transfer boundary, canonical format rules, Android Sharesheet/FilePicker behavior, transactional import, and explicit encryption/merge exclusions. Add the acceptance criteria and verification commands to the active plan without claiming manual validation that was not performed.

- [ ] **Step 2: Review the complete diff**

  Check for raw health-data logging, storage permissions, localized values in the file format, reused external IDs, partial-import paths, stale resource keys, direct repository calls from ViewModels, and unrelated changes. Run `git diff --check` and confirm only the transfer feature files changed.

- [ ] **Step 3: Run the full test suite**

  ```powershell
  dotnet restore
  dotnet test --configuration Release
  ```

  Expected: all Domain, Application, Infrastructure, and App tests pass.

- [ ] **Step 4: Build Android in Debug and Release**

  Run the repository’s Android build with the configured SDK/JDK overrides and confirm 0 warnings and 0 errors. The Release build must also complete the MAUI linker step successfully.

- [ ] **Step 5: Perform the Android smoke test**

  On the emulator or connected Pixel: create a profile with female and male measurements, export each profile, confirm the Android Sharesheet opens, import one CSV from Downloads or a cloud provider, confirm the preview, verify the imported profile appears under a new ID with history/results intact, cancel one import, and verify the four-profile limit disables the import action.

- [ ] **Step 6: Commit the documentation and verification slice**

  ```powershell
  git add ARCHITECTURE.md PLAN.md docs/superpowers/specs/2026-09-19-profile-csv-transfer-design.md docs/superpowers/plans/2026-09-19-profile-csv-transfer-plan.md
  git commit -m "docs: define profile csv transfer acceptance"
  ```

## Final verification commands

```powershell
dotnet restore
dotnet test --configuration Release
dotnet build .\src\Anthropometry.App\Anthropometry.App.csproj -f net10.0-android -c Debug -m:1 -p:PublishTrimmed=false -p:RunAOTCompilation=false -p:AndroidSdkDirectory="$env:LOCALAPPDATA\Android\Sdk" -p:JavaSdkDirectory="C:\Program Files\Microsoft\jdk-21.0.12.101-hotspot" --verbosity minimal
```

## Execution record

- [x] Task 1 — versioned CSV transfer contract and serializer; 7 focused tests passed.
- [x] Task 2 — transactional SQLite transfer port; 16 Infrastructure tests passed.
- [x] Task 3 — export/import Application use cases; 55 Application tests passed.
- [x] Task 4 — Android cache/share/file-picker adapter; Android Debug build passed with 0 warnings and 0 errors.
- [x] Task 5 — profile commands, confirmation flow, localization, and UI; 144 App tests and Android Debug build passed with 0 warnings and 0 errors.
- [x] Task 6 — architecture/plan documentation, diff review, and automated verification.
- [ ] Android emulator/Pixel smoke validation — not performed in this session.

Execution ruling recorded in the ledger: the installed MAUI target has no cancellation-token overload on `Share.Default.RequestAsync` or `FilePicker.Default.PickAsync`, so the adapter uses `WaitAsync(cancellationToken)` around those native tasks.

Verification note: the full Release test suite and Android Release build pass with `PublishTrimmed=false` and `RunAOTCompilation=false`. The default trimmed Release test run is blocked by the installed MAUI linker task host (`MSB4216`/`MSB4027`); production trimming/AOT packaging remains a host/toolchain follow-up.
