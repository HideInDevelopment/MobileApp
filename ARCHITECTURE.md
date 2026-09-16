# Anthropometric Tracking App Architecture

## 1. Purpose

This mobile application will let users create multiple personal profiles, record anthropometric measurements, and calculate results locally without relying on an Internet connection.

The initial functional scope includes:

- Body-fat percentage using gender-specific anthropometric equations: the male equation uses neck, abdomen, and height; the female equation uses neck, waist, hip, and height.
- Basal metabolic rate (BMR).
- Total daily energy expenditure (TDEE) based on BMR and an activity level.
- Measurement and result history per profile.
- Local SQLite persistence.

The application will target Android first while keeping a structure that allows iOS to be added later without duplicating the domain or application logic.

## 2. Main decisions

| Area | Decision |
|---|---|
| Initial platform | Android-first |
| Framework | .NET MAUI on .NET 10 |
| Language | C# |
| UI | .NET MAUI with XAML and MVVM |
| MVVM state | `CommunityToolkit.Mvvm` |
| Database | Local SQLite in the app's private storage |
| Initial SQLite access | `sqlite-net-pcl`, hidden behind application-owned interfaces |
| Architecture | Pragmatic Clean Architecture with domain-oriented boundaries |
| Domain design | Entities, value objects, use cases, and calculation strategies |
| Testing | TDD for domain and application; infrastructure tests and critical UI-flow tests |
| Network | Out of scope for the MVP; no backend or synchronization |
| Canonical units | Metric system (kilograms and centimeters) |
| Display units | Persisted Presentation preference: Metric (m/cm/kg) or Imperial (decimal ft/in/lb) |

.NET MAUI is the evolution of Xamarin.Forms and allows code sharing between Android and iOS while retaining access to native APIs when needed. See the [official .NET MAUI documentation](https://learn.microsoft.com/en-us/dotnet/maui/?view=net-maui-10.0).

The choice of .NET MAUI addresses three needs: keeping C# as the primary language, avoiding a domain rewrite if iOS is added, and keeping the solution simple enough for a local-first application. If the product became exclusively Android-focused with highly platform-specific requirements, Kotlin + Jetpack Compose would be a valid alternative, but it is not the baseline choice for this project.

## 3. Design principles

### 3.1 Reuse

- Business logic lives in .NET projects independent from MAUI.
- Formulas are implemented as reusable strategies, not inside pages or ViewModels.
- Unit conversion is centralized in domain or application services.
- Repeated visual components become shared controls or resources only after a real repetition appears.
- Reuse must not introduce speculative abstractions.

### 3.2 SOLID

- Each entity, use case, repository, and formula has one clear responsibility.
- Dependencies point to abstractions defined by inner layers.
- Formulas can be replaced or extended without changing navigation or persistence.
- Interfaces are small and consumer-oriented.
- Dependency composition happens at one application entry point.

### 3.3 Clean Architecture

Outer layers depend on inner layers, never the other way around:

```text
┌──────────────────────────────────────────────┐
│ Presentation: MAUI, XAML, ViewModels          │
└──────────────────────┬───────────────────────┘
                       │ uses use cases
┌──────────────────────▼───────────────────────┐
│ Application: use cases, ports, DTOs           │
└──────────────────────┬───────────────────────┘
                       │ uses domain rules
┌──────────────────────▼───────────────────────┐
│ Domain: entities, value objects, formulas     │
└──────────────────────────────────────────────┘

Infrastructure implements Application ports and is connected at the
composition root of the MAUI project.
```

The architecture is deliberately pragmatic: no mediator, bus, generic repository, or empty module will be added without a concrete need.

### 3.4 Domain-Driven Design

The domain will use the product's own language:

- profile;
- measurement;
- calculation result;
- calculation type;
- formula and version;
- activity level;
- measurement unit.

Important business rules will be protected by domain types and validation. The UI may improve data entry, but it will never be the only place where data is validated.

### 3.5 Test-Driven Development

Formulas and use cases will follow a Red-Green-Refactor cycle:

1. write a test describing the behavior;
2. confirm that it fails for the expected reason;
3. implement the smallest solution;
4. confirm that it passes;
5. refactor without changing behavior.

Domain tests must not depend on an emulator, Android, or SQLite.

## 4. Proposed solution structure

When the solution is created, use this structure:

```text
src/
  Anthropometry.Domain/
    Common/
    Profiles/
    Measurements/
    Calculations/

  Anthropometry.Application/
    Abstractions/
    Profiles/
    Measurements/
    Calculations/
    Common/

  Anthropometry.Infrastructure/
    Persistence/
      Sqlite/
      Migrations/
    Repositories/
    Services/

  Anthropometry.App/
    Features/
      Profiles/
      Measurements/
      Results/
    Components/
    Resources/
    Platforms/Android/
    MauiProgram.cs

tests/
  Anthropometry.Domain.Tests/
  Anthropometry.Application.Tests/
  Anthropometry.Infrastructure.Tests/
  Anthropometry.App.Tests/
```

Organizing Presentation by feature avoids a global ViewModels folder that eventually mixes unrelated flows. Domain and Application may combine concept-oriented and use-case-oriented organization as long as each file keeps one focused responsibility.

## 5. Domain model

### 5.1 Profile

Represents a person or tracking context.

Minimum properties:

- `ProfileId`, stable identifier.
- `Name`, non-empty with a defined maximum length.
- `Gender`, a required `Male` or `Female` value used to select the profile's formula strategies.
- `Settings`, containing the current height in centimeters, age in years, and named activity level.
- `CreatedAtUtc`.
- `UpdatedAtUtc`.

New and edited profiles require a valid gender and valid settings. Existing profiles are migrated with `Male` as the compatibility default until the user edits them. `Settings` may be absent only on a legacy profile that predates the settings migration and must be completed before recording a new measurement. The profile will not store passwords or authentication data. It may be deleted together with its measurements and results through an explicit operation.

### 5.2 Measurement

Represents one data capture associated with a profile.

Minimum data:

- `MeasurementId`.
- `ProfileId`.
- `MeasuredAtUtc`.
- `MeasurementType`, either `WeightOnly` or `WeightAndSizes`.
- `WeightKg`.
- `HeightCm`, copied from the profile settings at capture time.
- `NeckCm` and `AbdomenCm`, required for `WeightAndSizes` and absent for `WeightOnly`. For a female measurement, `AbdomenCm` stores the waist value used by the female equation.
- `HipCm`, required for a female `WeightAndSizes` record and absent for `WeightOnly`.
- `Gender`, copied from the profile at capture time so later calculations use the gender selected for that measurement.
- `AgeYears`, captured in the measurement so the age used by historical calculations is preserved.
- `ActivityLevel`, captured in the measurement so later profile edits do not change historical context.

`WeightOnly` records a new weight and timestamp without storing new size values. When an earlier `WeightAndSizes` measurement exists, the application recalculates body-fat, BMR, and TDEE for the new weight using that earlier measurement's required size values; the results remain attached to the new measurement. `WeightAndSizes` records the required gender-specific sizes and creates the same three results through the existing versioned calculation pipeline. The measurement must preserve the entered values, not only derived results. This allows recalculation, auditing, and adding new formulas later.

### 5.3 CalculationResult

Represents the result produced by one specific formula.

Minimum properties:

- `CalculationResultId`.
- `MeasurementId`.
- `CalculationType`.
- `FormulaId`.
- `FormulaVersion`.
- `Value`.
- `Unit`.
- `CalculatedAtUtc`.

The formula and version are stored with the result so that history does not silently change when a later implementation is corrected.

### 5.4 Value objects and controlled types

Use domain types where they prevent ambiguous primitives:

- `ProfileId`.
- `MeasurementId`.
- `Weight`.
- `Length`.
- `Age`.
- `ActivityLevel`.
- `CalculationType`.
- `CalculationResult`.

Do not create a type for every number on day one. Add value objects where they provide meaningful validation, unit semantics, or domain meaning.

## 6. Calculation engine

Formulas must not be mixed with persistence or UI code. The engine will use strategies registered by calculation type:

```csharp
public interface ICalculationFormula<in TInput, TResult>
{
    CalculationType Type { get; }
    string FormulaId { get; }
    string Version { get; }
    Result<TResult> Calculate(TInput input);
}
```

The exact contract may be refined during implementation, but it must preserve these concepts: type, identity, version, typed inputs, and a validated result.

### 6.1 Body-fat percentage

The male implementation uses the US Navy formula based on abdomen, neck, and height. Values entered in centimeters will be converted to inches before applying the formula because its constants are defined for inches:

```text
bodyFatPercentage =
    86.010 × log10(abdomenInches - neckInches)
  - 70.041 × log10(heightInches)
  + 36.76
```

The UI states that this formula is for men and that the abdomen must be measured at the product-defined location. The formula rejects non-positive inputs and any case where `abdomenInches - neckInches` is not greater than zero.

The Presentation layer exposes one persisted Metric/Imperial preference. Metric displays profile height in meters, circumferences in centimeters, and weight in kilograms. Imperial displays profile height in decimal feet, circumferences in inches, and weight in pounds. All values are converted back to canonical centimeters or kilograms before they cross into Application, and no feet-and-inches shorthand is used.

The female implementation uses the classic US Navy/Hodgdon-Beckett equation and requires waist, hip, neck, and height. Metric values are converted to inches at the formula boundary:

```text
bodyFatPercentage =
    163.205 × log10((waistCm + hipCm - neckCm) / 2.54)
  - 97.684 × log10(heightCm / 2.54)
  - 78.387
```

The female formula rejects non-positive inputs and any case where `waistCm + hipCm - neckCm` is not greater than zero. It is a separate formula identity and version; it does not replace historical male results.

### 6.2 Basal metabolic rate

The male implementation uses the Mifflin-St Jeor equation:

```text
BMR = 10 × weightKg + 6.25 × heightCm - 5 × ageYears + 5
```

The female implementation uses the corresponding Mifflin-St Jeor variant:

```text
BMR = 10 × weightKg + 6.25 × heightCm - 5 × ageYears - 161
```

Both variants are identified and versioned separately. The female identity is `mifflin-st-jeor-female-bmr`, version `1.0`; it does not alter historical male results.

### 6.3 Total daily energy expenditure

TDEE will be calculated explicitly for both genders as:

```text
TDEE = BMR × activity factor
```

The MVP will offer these levels and factors:

| Level | Factor |
|---|---:|
| Sedentary | 1.2 |
| Light | 1.375 |
| Moderate | 1.55 |
| High | 1.725 |
| Very high | 1.9 |

The activity factor will not be stored as an anonymous UI number. It will be represented by an enum or value object with a label, description, and factor defined by the formula/version. There is no separate female TDEE equation in this slice; the female BMR is multiplied by the selected activity factor.

### 6.4 Versioning and precision

- Every formula has a stable identity and version.
- Internal values use enough precision to avoid premature rounding.
- Rounding is applied at presentation or final-result boundaries according to the unit.
- Rounding rules have explicit tests.
- Changing a formula creates a new version instead of silently mutating the old one.

## 7. Use cases

Application will expose small, testable use cases:

- `CreateProfile`.
- `UpdateProfile`.
- `DeleteProfile`.
- `GetProfiles`.
- `RecordMeasurement`.
- `GetMeasurementHistory`.
- `CalculateBodyFat`.
- `CalculateBasalMetabolicRate`.
- `CalculateTotalDailyEnergyExpenditure`.

A use case may coordinate several ports, but it must not contain SQL or references to MAUI. Application DTOs prevent SQLite objects from leaking into Presentation.

Initial ports:

```csharp
public interface IProfileRepository
{
    Task<IReadOnlyList<Profile>> GetAllAsync(CancellationToken cancellationToken);
    Task<Profile?> GetByIdAsync(ProfileId id, CancellationToken cancellationToken);
    Task AddAsync(Profile profile, CancellationToken cancellationToken);
    Task UpdateAsync(Profile profile, CancellationToken cancellationToken);
    Task DeleteAsync(ProfileId id, CancellationToken cancellationToken);
}

public interface IMeasurementRepository
{
    Task AddAsync(Measurement measurement, CancellationToken cancellationToken);
    Task<IReadOnlyList<Measurement>> GetByProfileAsync(
        ProfileId profileId,
        CancellationToken cancellationToken);
}

public interface ICalculationResultRepository
{
    Task AddAsync(CalculationResult result, CancellationToken cancellationToken);
    Task<IReadOnlyList<CalculationResult>> GetByMeasurementAsync(
        MeasurementId measurementId,
        CancellationToken cancellationToken);
}
```

These signatures are an initial guide; the definitive contracts will be fixed while implementing the first slice.

## 8. SQLite persistence

### 8.1 Infrastructure responsibilities

Infrastructure is responsible for:

- opening the database in the app's private directory;
- creating and migrating the schema;
- mapping rows to entities and back;
- applying transactions;
- serializing complex values explicitly;
- returning infrastructure failures in a form that Application can translate.

### 8.2 Initial tables

```text
Profiles
  Id
  Name
  HeightCm (nullable for legacy incomplete profiles)
  AgeYears (nullable for legacy incomplete profiles)
  ActivityLevel (nullable for legacy incomplete profiles)
  CreatedAtUtc
  UpdatedAtUtc

Measurements
  Id
  ProfileId
  MeasuredAtUtc
  MeasurementType
  WeightKg
  HeightCm
  NeckCm (nullable for WeightOnly)
  AbdomenCm (nullable for WeightOnly)
  HipCm (nullable for WeightOnly and male measurements)
  Gender
  AgeYears
  ActivityLevel

CalculationResults
  Id
  MeasurementId
  CalculationType
  FormulaId
  FormulaVersion
  Value
  Unit
  CalculatedAtUtc

SchemaMetadata
  Key
  Value
```

Tables must have indexes for `ProfileId`, `MeasurementId`, and measurement dates. Referential integrity will be implemented explicitly rather than relying only on library conventions. Deleting a profile will delete its measurements and associated results in one transaction; the UI must ask for confirmation before executing the operation.

### 8.3 Migrations

Every schema change has a version and a migration test from the previous version. The current schema is version 4: version 2 adds persisted profile settings, adds `MeasurementType`, and makes neck and abdomen nullable for weight-only records; version 3 adds profile gender; version 4 adds measurement hip values and a gender snapshot, defaulting legacy measurements to `Male`. Existing measurements remain readable, and profile settings are backfilled from each profile's latest measurement when possible. Updating the application must not lose user data.

The database will initialize asynchronously before the first screen depends on it. An initialization failure must prevent operation with incomplete data and show a recoverable error screen.

## 9. Presentation and user experience

Initial navigation:

```text
Profile list
    ├── Create profile
    └── Profile detail
          ├── Add weight
          ├── Add measurements
          ├── Current result
          └── History
```

Each screen has a ViewModel that:

- exposes observable properties;
- runs asynchronous commands;
- translates Application results into UI states;
- does not know about SQLite;
- does not implement formulas;
- can cancel operations when the screen is no longer active.

Minimum states for each flow:

- initializing;
- loading;
- empty;
- ready;
- invalid input;
- recoverable error;
- operation completed.

The profile list shows a centered create action when no profiles exist. Once profiles exist, `Add profile` appears in the top area and remains visible but disabled after four profiles. The profile detail screen enables `Add weight` only after at least one size-based measurement exists and does not show a warning icon. After either measurement save, the editor closes and the app opens the refreshed History view. History formats local dates using the selected `dd/MM/yyyy` or `MM/dd/yyyy` order and displays weight and height using the selected Presentation units. It offers results for both measurement types; weight-only rows show a centered warning icon before the date and a yellow background because their results reuse the previous neck and abdomen values. History also exposes a ruler toolbar action with a `Weight graphic` option; the graphic plots every persisted weight measurement chronologically as points joined by a line, and tapping a point shows its date and weight legend until another chart location is tapped. The chart uses the selected weight unit and date format for display. Profile detail also exposes a temporary `Generate sample data` action that creates a deterministic 30-day alternating history from the latest size-based measurement, including persisted calculation results. The toolbar uses icon-only Settings and Help actions; Settings opens a Presentation-only settings screen where English, Spanish, and German can be selected, along with the date order and the unified Metric/Imperial measurement system. The selected language, appearance, and display preferences are persisted in local MAUI Preferences and restored before the first feature page is created.

The visual style will be minimal and functional:

- clear visual hierarchy;
- few colors used consistently;
- short forms grouped by concept;
- units shown next to fields;
- prominent results without visual overload;
- messages that explain how to correct a value;
- suitable touch targets and support for larger text.

The UI will use `Grid`, `VerticalStackLayout`, `ScrollView`, `CollectionView`, and shared styles. Absolute positioning is avoided unless there is a specific visual reason. Screens will be tested at small and large sizes, with the keyboard visible, and in every supported orientation.

Localization resources, language preference, appearance preference, and display-unit preference remain in Presentation. ViewModels consume Presentation services for derived labels, errors, conversions, and formatting, while XAML uses dynamic resource keys for static copy. Domain and Application do not reference cultures, resource files, MAUI, or Preferences.

## 10. Dependency injection and configuration

`MauiProgram.cs` is the composition root. It registers:

- database services;
- schema migrator;
- repositories;
- formulas;
- use cases;
- clock and unit services;
- ViewModels and pages.

Domain code does not use the dependency-injection container. Classes receive dependencies through constructors.

Tests replace real implementations with controlled doubles, in-memory repositories, or temporary SQLite databases.

## 11. Validation, errors, and privacy

### Validation

Domain validation checks:

- positive values;
- physiologically acceptable ranges defined by product;
- consistent units;
- required data present;
- mathematical preconditions for each formula;
- compatibility between profile, measurement, and calculation.

Presentation validation improves immediate form feedback but never replaces domain validation.

### Errors

- Invalid data is returned as controlled result errors.
- Unexpected SQLite failures are logged locally without exposing technical details to the user.
- UI errors are short and actionable.
- A persistence failure is never hidden by pretending that saving completed.

### Privacy

- Data is stored in the app's private storage.
- No unnecessary permissions are requested.
- The MVP collects no analytics and sends no data to third parties.
- Secrets are never stored in the repository.
- Deleting a profile warns that its measurements and results will be removed.
- Product copy must state that the results are estimates, not medical diagnoses.

## 12. Testing strategy

### Domain.Tests

- known cases for every formula;
- valid boundaries;
- invalid inputs;
- precision and rounding;
- formula versioning;
- entity and value-object invariants.

### Application.Tests

- profile creation and editing;
- measurement recording;
- BMR + activity-factor composition;
- versioned-result persistence coordination;
- missing-data errors;
- cancellation where relevant.

### Infrastructure.Tests

- schema creation;
- migrations;
- entity mapping;
- indexes and profile queries;
- transactions;
- transactional deletion of profiles, measurements, and results.

### App.Tests

- ViewModels in loading, empty, success, and error states;
- form validation;
- navigation through critical flows;
- selective UI tests for creating a profile, recording a measurement, and viewing a result.

## 13. Planned evolution

The design leaves room for these future additions, but they are not part of the MVP:

- more anthropometric equations;
- profiles with formula-specific sex or parameters;
- additional progress charts beyond the initial weight graphic;
- CSV or PDF export;
- local backup;
- optional synchronization;
- iOS;
- reminders and notifications.

Each extension must add a use case and tests first, then adapt Presentation. Remote infrastructure will not be added until a product need justifies it.

## 14. Technical references

- [.NET MAUI documentation](https://learn.microsoft.com/en-us/dotnet/maui/?view=net-maui-10.0)
- [.NET MAUI supported platforms](https://learn.microsoft.com/en-us/dotnet/maui/supported-platforms?view=net-maui-10.0)
- [Jetpack Compose documentation](https://developer.android.com/develop/ui/compose/documentation) — considered native Android alternative
