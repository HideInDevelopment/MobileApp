# Profile Transfer Encryption Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace new plain-CSV profile exports with password-protected `.anthropometry` files that provide confidentiality and authenticated tamper detection while preserving the existing validated profile import workflow.

**Architecture:** The Application layer will protect the existing canonical CSV payload with a versioned AES-256-GCM envelope and derive the per-file key from a user passphrase with PBKDF2-HMAC-SHA256. Presentation will collect the passphrase through an obscured MAUI page, while the file adapter will handle only bounded bytes and Android file/share APIs; no Android Keystore key, SQLite migration, or external service is introduced.

**Tech Stack:** .NET 10 BCL cryptography (`AesGcm`, `Rfc2898DeriveBytes.Pbkdf2`, `RandomNumberGenerator`, `CryptographicOperations`), .NET MAUI/XAML, existing Application/Infrastructure transfer use cases, `CommunityToolkit.Mvvm`, xUnit, Android Sharesheet, Android Storage Access Framework.

**Spec:** `docs/superpowers/specs/2026-09-20-profile-transfer-encryption-design.md`

## Global Constraints

- New exports use the `.anthropometry` extension and contain a binary envelope, not readable CSV.
- The inner payload remains the existing versioned UTF-8 CSV with canonical kilograms, centimetres, invariant decimals, and UTC timestamps.
- Use PBKDF2-HMAC-SHA256 with 600,000 iterations, a 16-byte random salt, and a 32-byte derived key.
- Use AES-256-GCM with a 12-byte random nonce and a 16-byte authentication tag.
- Reject protected files larger than 10 MiB and PBKDF2 iteration counts outside the supported safe bounds before expensive work.
- Require a passphrase of at least 12 non-whitespace characters; never persist, log, or hard-code it.
- Authenticate the envelope header through the nonce as AES-GCM associated data.
- Import must decrypt, authenticate, parse, validate, preview, confirm, and then use the existing single SQLite transaction.
- Keep temporary secret byte arrays best-effort cleared with `CryptographicOperations.ZeroMemory`.
- Accept previous plain CSV exports only as an explicitly labelled legacy path; never generate new CSV exports.
- Do not add packages, Android storage permissions, account services, cloud synchronization, or digital signatures.

## Review Focus

- A one-byte change to ciphertext, tag, salt, nonce, or authenticated header must fail before any database write; pin this in protection and import tests.
- A hostile envelope with unsupported versions, invalid lengths, oversized payload, or extreme iteration count must be rejected before allocation or unbounded KDF work; pin bounds tests in the protection task.
- Wrong or missing passphrases and mismatched export confirmation must be recoverable cancellation/error states with no plaintext logging or persistence; pin Application and App tests in the use-case/UI tasks.
- A valid legacy CSV must be imported only with an explicit unprotected warning, while all new exports use `.anthropometry`; pin compatibility and filename tests in the Application task.
- A protected female profile containing weight-only history and historical formula metadata must round-trip unchanged after decrypting; pin the existing transfer fixture in the integration task.

---

### Task 1: Implement the versioned authenticated envelope

**Files:**
- Create: `src/Anthropometry.Application/Profiles/ProfileTransferProtection.cs`
- Modify: `src/Anthropometry.Application/Profiles/ProfileTransferModels.cs`
- Modify: `src/Anthropometry.Application/Common/ApplicationErrors.cs`
- Create: `tests/Anthropometry.Application.Tests/Profiles/ProfileTransferProtectionTests.cs`

**Interfaces:**
- Produce `public sealed record ProfileTransferProtectedPayload(byte[] CsvContent, bool IsLegacyUnprotected)`.
- Produce `public static Result<byte[]> ProfileTransferProtection.Protect(byte[] csvContent, string passphrase)`.
- Produce `public static Result<ProfileTransferProtectedPayload> ProfileTransferProtection.Unprotect(byte[] content, string? passphrase)`.
- Produce `public static Result ValidatePassphrase(string? passphrase)`.
- Keep `ProfileTransferCsvSerializer` unchanged; the protector receives and returns bytes and never parses domain data.

- [ ] **Step 1: Write failing cryptography tests**

  Add tests for protected round-trip, fresh salt/nonce on repeated protection, wrong passphrase, tampering of ciphertext/tag/salt/nonce/authenticated header, unsupported envelope/algorithm versions, invalid lengths, oversized payload, iteration bounds, short/blank passphrases, legacy CSV detection, and unrelated bytes. Assert controlled error codes rather than leaked cryptographic exceptions.

- [ ] **Step 2: Run the focused tests and verify the expected red state**

  ```powershell
  dotnet test .\tests\Anthropometry.Application.Tests\Anthropometry.Application.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~ProfileTransferProtectionTests"
  ```

  Expected: compilation failures because the protector and protected payload contract do not exist yet.

- [ ] **Step 3: Implement the minimum envelope and key derivation**

  Define the binary envelope in the exact order from the spec: magic, envelope version, inner CSV version, KDF ID, cipher ID, big-endian iteration count, 16-byte salt, 12-byte nonce, 16-byte tag, and ciphertext. Use `RandomNumberGenerator.Fill`, `Rfc2898DeriveBytes.Pbkdf2` with `HashAlgorithmName.SHA256` and 32 output bytes, and `AesGcm.Encrypt`/`Decrypt` with the header-through-nonce bytes as associated data. Enforce a 10 MiB total input bound, a non-empty payload, supported IDs, and bounded iterations before KDF work. Detect the existing CSV header as legacy only when the input is not an envelope. Return one generic authentication error for wrong passwords and tampering. Clear owned derived-key, nonce, salt-copy, and password-byte buffers in `finally` blocks.

- [ ] **Step 4: Run the focused tests and refactor only for clarity**

  Re-run the focused command. Expected: all protection tests pass with no Android, MAUI, SQLite, or new package dependency. Confirm attacker-controlled lengths are checked before allocation.

- [ ] **Step 5: Commit the cryptography slice**

  ```powershell
  git add src/Anthropometry.Application/Profiles/ProfileTransferProtection.cs src/Anthropometry.Application/Profiles/ProfileTransferModels.cs src/Anthropometry.Application/Common/ApplicationErrors.cs tests/Anthropometry.Application.Tests/Profiles/ProfileTransferProtectionTests.cs
  git commit -m "feat: protect profile transfer files"
  ```

---

### Task 2: Integrate protected payloads into Application export/import

**Files:**
- Modify: `src/Anthropometry.Application/Profiles/ExportProfile.cs`
- Modify: `src/Anthropometry.Application/Profiles/ImportProfile.cs`
- Modify: `src/Anthropometry.Application/Profiles/ProfileTransferModels.cs`
- Modify: `src/Anthropometry.Application/Common/ApplicationErrors.cs`
- Modify: `tests/Anthropometry.Application.Tests/Profiles/ProfileTransferUseCaseTests.cs`
- Modify: `tests/Anthropometry.Application.Tests/Profiles/ProfileTransferCsvSerializerTests.cs`

**Interfaces:**
- Replace `ExportProfileCommand(ProfileId ProfileId)` with `ExportProfileCommand(ProfileId ProfileId, string Passphrase)`.
- Keep `ExportProfile.ExecuteAsync(ExportProfileCommand, CancellationToken) -> Task<Result<ProfileExportFile>>`.
- Change `ImportProfile.PreviewAsync` to `Task<Result<ProfileImportPreview>> PreviewAsync(byte[] content, string? passphrase, CancellationToken cancellationToken)`.
- Extend `ProfileImportPreview` with `bool IsLegacyUnprotected`.
- Add localized Application errors for password required, authentication failure, and invalid passphrase.

- [ ] **Step 1: Update tests for the protected use-case contract**

  Update existing fixtures to provide a valid passphrase and assert that exported filenames end in `.anthropometry`, exported bytes are protected, and decrypted content preserves the same profile, measurement, and result information. Add cases for empty/short passphrases, password-required preview, wrong-password failure, legacy CSV preview, protected female/weight-only round trip, and no repository call after failed authentication.

- [ ] **Step 2: Run focused Application tests and confirm the expected red state**

  ```powershell
  dotnet test .\tests\Anthropometry.Application.Tests\Anthropometry.Application.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~ProfileTransferUseCaseTests|FullyQualifiedName~ProfileTransferCsvSerializerTests"
  ```

  Expected: signature compilation failures and failed assertions for protected filename/content behavior.

- [ ] **Step 3: Protect exports and validate passphrases in the Application layer**

  Make `ExportProfile` validate the command passphrase, serialize the complete snapshot with the existing serializer, call `ProfileTransferProtection.Protect`, and return `anthropometry-<safe-name>-<yyyyMMdd>.anthropometry`. Keep snapshot and result metadata behavior unchanged. Do not log or persist the passphrase.

- [ ] **Step 4: Decrypt, validate, and mark legacy imports before persistence**

  Make `ImportProfile.PreviewAsync` call `ProfileTransferProtection.Unprotect` before `ProfileTransferCsvSerializer.Parse`. Map missing passphrase and authentication failure to controlled errors, parse the returned CSV, and carry `IsLegacyUnprotected` into the preview. Keep `ExecuteAsync` dependent only on the validated preview, preserve new-ID remapping, the four-profile limit, formula metadata, and one transactional repository call.

- [ ] **Step 5: Run all Application tests**

  ```powershell
  dotnet test .\tests\Anthropometry.Application.Tests\Anthropometry.Application.Tests.csproj --configuration Debug --no-restore --verbosity minimal
  ```

  Expected: all existing Application tests plus protected transfer cases pass.

- [ ] **Step 6: Commit the Application integration slice**

  ```powershell
  git add src/Anthropometry.Application/Profiles/ExportProfile.cs src/Anthropometry.Application/Profiles/ImportProfile.cs src/Anthropometry.Application/Profiles/ProfileTransferModels.cs src/Anthropometry.Application/Common/ApplicationErrors.cs tests/Anthropometry.Application.Tests/Profiles/ProfileTransferUseCaseTests.cs tests/Anthropometry.Application.Tests/Profiles/ProfileTransferCsvSerializerTests.cs
  git commit -m "feat: integrate protected profile transfers"
  ```

---

### Task 3: Update the Android file adapter and secure passphrase prompt

**Files:**
- Create: `src/Anthropometry.App/Features/Profiles/ProfileTransferFile.cs`
- Create: `src/Anthropometry.App/Features/Profiles/PassphrasePromptPage.xaml`
- Create: `src/Anthropometry.App/Features/Profiles/PassphrasePromptPage.xaml.cs`
- Create: `src/Anthropometry.App/Features/Profiles/PassphrasePromptViewModel.cs`
- Modify: `src/Anthropometry.App/Features/Profiles/IProfileTransferFileService.cs`
- Modify: `src/Anthropometry.App/Features/Profiles/MauiProfileTransferFileService.cs`
- Modify: `src/Anthropometry.App/Anthropometry.App.csproj`
- Create: `tests/Anthropometry.App.Tests/Features/Profiles/PassphrasePromptViewModelTests.cs`
- Modify: `tests/Anthropometry.App.Tests/Features/Profiles/ProfileTransferMarkupTests.cs`

**Interfaces:**
- Produce `public sealed record ProfileTransferFile(string FileName, byte[] Content)`.
- Change `IProfileTransferFileService.PickCsvAsync` to `Task<ProfileTransferFile?> PickTransferAsync(string title, CancellationToken cancellationToken)`.
- Keep `ShareAsync(ProfileExportFile file, string title, CancellationToken cancellationToken) -> Task`.
- The prompt page returns either a passphrase or cancellation through a `TaskCompletionSource<string?>`; Entry controls use `IsPassword="True"`.

- [ ] **Step 1: Write failing prompt and adapter contract tests**

  Add ViewModel tests for accepting a matching passphrase, rejecting mismatched confirmation, rejecting a too-short passphrase, and cancelling without returning a secret. Extend markup tests for `IsPassword="True"`, the `.anthropometry` export binding, and the protected import command path.

- [ ] **Step 2: Run focused App tests and verify the expected red state**

  ```powershell
  dotnet test .\tests\Anthropometry.App.Tests\Anthropometry.App.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~PassphrasePromptViewModelTests|FullyQualifiedName~ProfileTransferMarkupTests"
  ```

  Expected: compilation or assertion failures because the secure prompt and renamed adapter contract do not exist yet.

- [ ] **Step 3: Implement the bounded Android file adapter**

  Keep exports in `FileSystem.CacheDirectory`, share `.anthropometry` through `ShareFileRequest`, and pick `.anthropometry` plus `.csv` legacy files through the Android FilePicker/Storage Access Framework. Copy the selected stream into a bounded byte array, reject content over 10 MiB, and return `ProfileTransferFile`. Continue using `WaitAsync(cancellationToken)` around MAUI APIs because the installed target has no cancellation-token overload. Do not request storage permissions or add a package.

- [ ] **Step 4: Implement the obscured passphrase prompt**

  Add a small modal page with localized title/instructions, password Entry, optional confirmation Entry for export, and explicit Save/Cancel buttons. The ViewModel validates the passphrase through the Application rule, exposes a recoverable error, and clears bound strings after completion/cancellation where practical. The page completes exactly once and returns no value on dismissal.

- [ ] **Step 5: Run focused tests and Android compile verification**

  ```powershell
  dotnet test .\tests\Anthropometry.App.Tests\Anthropometry.App.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~PassphrasePromptViewModelTests|FullyQualifiedName~ProfileTransferMarkupTests"
  dotnet build .\src\Anthropometry.App\Anthropometry.App.csproj -f net10.0-android -c Debug -m:1 -p:PublishTrimmed=false -p:RunAOTCompilation=false -p:AndroidSdkDirectory="$env:LOCALAPPDATA\Android\Sdk" -p:JavaSdkDirectory="C:\Program Files\Microsoft\jdk-21.0.12.101-hotspot" --verbosity minimal
  ```

  Expected: focused tests pass and Android builds with 0 warnings and 0 errors.

- [ ] **Step 6: Commit the adapter and prompt slice**

  ```powershell
  git add src/Anthropometry.App/Features/Profiles src/Anthropometry.App/Anthropometry.App.csproj tests/Anthropometry.App.Tests/Features/Profiles
  git commit -m "feat: add protected transfer password flow"
  ```

---

### Task 4: Orchestrate protected export/import and localization

**Files:**
- Modify: `src/Anthropometry.App/MauiNavigation.cs`
- Modify: `src/Anthropometry.App/Localization/LanguageService.cs`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.resx`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.es.resx`
- Modify: `src/Anthropometry.App/Resources/Strings/AppResources.de.resx`
- Modify: `tests/Anthropometry.App.Tests/Features/Profiles/ProfileTransferMarkupTests.cs`
- Modify: `tests/Anthropometry.App.Tests/Features/Profiles/ProfileListViewModelTests.cs`
- Modify: `tests/Anthropometry.App.Tests/Features/Profiles/ProfileDetailViewModelTests.cs`

**Interfaces:**
- `MauiNavigation.ExportProfileAsync` requests privacy confirmation, collects/confirm a passphrase, invokes `ExportProfileCommand(ProfileId, Passphrase)`, and then calls `ShareAsync`.
- `MauiNavigation.ImportProfileAsync` picks a `ProfileTransferFile`, requests a passphrase only when preview reports `ProfileTransferPasswordRequired`, retries using the same bytes, shows the legacy warning when applicable, then confirms and executes.
- `ProfileListViewModel.ImportCommand` reloads only after the navigation flow returns; cancellation and failures leave the current list unchanged.

- [ ] **Step 1: Add failing navigation-flow tests**

  Extend test doubles to record export passphrases, selected transfer files, preview retries, legacy-warning confirmation, cancellation, and share invocation. Assert wrong passwords do not call `ExecuteAsync`, export cancellation does not share, and successful import reloads once.

- [ ] **Step 2: Run focused tests and verify the expected red state**

  ```powershell
  dotnet test .\tests\Anthropometry.App.Tests\Anthropometry.App.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~ProfileTransferMarkupTests|FullyQualifiedName~ProfileListViewModelTests|FullyQualifiedName~ProfileDetailViewModelTests"
  ```

- [ ] **Step 3: Implement export orchestration**

  Show the privacy warning before asking for a passphrase. Open the obscured prompt in confirmation mode, call the protected export use case only after both entries match, and pass the localized title to the existing Sharesheet adapter. Map validation and share failures to localized messages without including the passphrase or file bytes.

- [ ] **Step 4: Implement import orchestration**

  Pick transfer bytes, call preview with no passphrase, and when password-required is returned, open the prompt and retry with the same bytes. Show profile name/counts and an explicit unprotected-legacy warning when applicable. Execute only after confirmation; map authentication, invalid-file, unsupported-format, limit, and persistence errors without clearing the current list.

- [ ] **Step 5: Add all localized resources**

  Add English, Spanish, and German keys for passphrase title/instructions, confirmation, mismatch, minimum length, password-required, authentication failure, invalid protected file, legacy warning, export/import actions, and success/cancellation. Register every key in `LanguageService.ResourceKeys`.

- [ ] **Step 6: Run the complete App suite**

  ```powershell
  dotnet test .\tests\Anthropometry.App.Tests\Anthropometry.App.Tests.csproj --configuration Debug --no-restore --verbosity minimal
  ```

  Expected: all App tests pass in the plain `net10.0` target.

- [ ] **Step 7: Commit the Presentation slice**

  ```powershell
  git add src/Anthropometry.App/MauiNavigation.cs src/Anthropometry.App/Features/Profiles src/Anthropometry.App/Localization/LanguageService.cs src/Anthropometry.App/Resources/Strings tests/Anthropometry.App.Tests/Features/Profiles
  git commit -m "feat: add protected profile transfer UX"
  ```

---

### Task 5: Update architecture, plan, and verify the complete feature

**Files:**
- Modify: `ARCHITECTURE.md`
- Modify: `PLAN.md`
- Modify: `docs/superpowers/specs/2026-09-19-profile-csv-transfer-design.md` with a historical/superseded note if needed
- Modify: `docs/superpowers/specs/2026-09-20-profile-transfer-encryption-design.md` only if implementation decisions changed during review
- Modify: `docs/superpowers/plans/2026-09-20-profile-transfer-encryption-plan.md` to record completed steps

- [x] **Step 1: Update the architecture boundary**

  Replace the current plain-CSV export description with the protected `.anthropometry` envelope, passphrase-derived key, AES-GCM authentication, legacy CSV migration path, no-Keystore portable-key decision, no-storage-permission rule, and explicit limitation that a passphrase holder can create a new valid file.

- [x] **Step 2: Review the complete diff**

  Run `git diff --check` and inspect for plaintext health-data logging, passphrase persistence, hard-coded keys, unauthenticated encryption, unbounded KDF work, file-size bypasses, stale `.csv` filenames, stale resource keys, direct repository calls from ViewModels, Android permissions, and unrelated changes.

- [x] **Step 3: Run full automated verification**

  ```powershell
  dotnet restore
  dotnet test --configuration Release -m:1 -p:PublishTrimmed=false -p:RunAOTCompilation=false --verbosity minimal
  dotnet build .\src\Anthropometry.App\Anthropometry.App.csproj -f net10.0-android -c Debug -m:1 -p:PublishTrimmed=false -p:RunAOTCompilation=false -p:AndroidSdkDirectory="$env:LOCALAPPDATA\Android\Sdk" -p:JavaSdkDirectory="C:\Program Files\Microsoft\jdk-21.0.12.101-hotspot" --verbosity minimal
  ```

  Expected: all Domain, Application, Infrastructure, and App tests pass; Android Debug builds with 0 warnings and 0 errors. Run the Release Android build with the same trim/AOT overrides and record any existing host linker limitation separately from feature failures.

- [ ] **Step 4: Perform the Android smoke test**

  On the emulator or connected Pixel, export male and female profiles, confirm the `.anthropometry` Sharesheet item, import from Downloads/document provider with the correct passphrase, verify history/results/formula metadata, retry with a wrong passphrase, modify one byte and confirm rejection, import a legacy CSV with its warning, cancel export/import, and confirm no partial profile appears.

- [x] **Step 5: Commit documentation and verification records**

  ```powershell
  git add ARCHITECTURE.md PLAN.md docs/superpowers/specs/2026-09-20-profile-transfer-encryption-design.md docs/superpowers/specs/2026-09-19-profile-csv-transfer-design.md docs/superpowers/plans/2026-09-20-profile-transfer-encryption-plan.md
  git commit -m "docs: define protected profile transfer acceptance"
  ```

## Final verification commands

```powershell
dotnet restore
dotnet test --configuration Release -m:1 -p:PublishTrimmed=false -p:RunAOTCompilation=false --verbosity minimal
dotnet build .\src\Anthropometry.App\Anthropometry.App.csproj -f net10.0-android -c Debug -m:1 -p:PublishTrimmed=false -p:RunAOTCompilation=false -p:AndroidSdkDirectory="$env:LOCALAPPDATA\Android\Sdk" -p:JavaSdkDirectory="C:\Program Files\Microsoft\jdk-21.0.12.101-hotspot" --verbosity minimal
```

## Execution record

- Task 1 complete in `4965625`: authenticated envelope and protection tests (14 passed after the final UTF-8 BOM compatibility regression test); Application suite (68 passed at task boundary).
- Task 2 complete in `69baca4`: protected export/import integration and transfer tests (16 focused passed); Application suite (70 passed).
- Task 3 complete in `cbde89f`: bounded Android adapter, obscured passphrase prompt, and prompt tests (10 focused passed); App suite (148 passed). Android compile was intentionally deferred until the shared navigation contract was updated in Task 4.
- Task 4 complete in `afb58c9`: protected navigation orchestration and localized English/Spanish/German copy; focused localization/markup tests (11 passed); App suite (149 passed); Android Debug build (0 warnings, 0 errors).
- Task 5 complete in the documentation commit: full Release tests passed (Domain 54, Application 70, Infrastructure 16, App 149); Android Debug and Release builds passed with 0 warnings and 0 errors using the documented trim/AOT overrides. Repository restore is blocked by missing host workload resolver SDK directories (`MSB4276`), and Android device smoke testing remains pending because it requires a connected emulator or Pixel.
