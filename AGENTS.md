# Agent Guidelines

## Mission

The code agent's purpose is to build and maintain an offline-first anthropometric tracking application for Android, using the architecture defined in `ARCHITECTURE.md`.

The agent is responsible for producing small, testable, reviewable changes that preserve domain correctness, user privacy, and the ability to add more formulas or a future iOS target without duplicating business logic.

These instructions are model-agnostic. They describe the expected behavior of any coding agent working in this repository and do not depend on a particular vendor, model, editor, or automation system.

## Product context

The application stores multiple user profiles and their measurements locally in SQLite. It calculates body-fat percentage, basal metabolic rate, and total daily energy expenditure without requiring Internet access.

The initial target is Android. The domain and application layers must remain independent of Android, .NET MAUI, XAML, and SQLite.

The application handles personal health-related estimates. It must make clear that results are estimates and not medical diagnoses.

## Required reading and source of truth

Before changing code, read:

1. `ARCHITECTURE.md` for architectural boundaries and domain decisions.
2. `PLAN.md` for the current implementation slice and acceptance criteria.
3. Existing tests and project configuration for conventions already established in the repository.

When a conflict exists, follow this priority:

1. Direct user instructions.
2. The approved requirements and `ARCHITECTURE.md`.
3. This file.
4. Existing implementation conventions that do not conflict with the items above.

## Non-negotiable architecture rules

- Domain code must not reference MAUI, Android, SQLite, XAML, or UI types.
- Application code must not execute SQL or reference MAUI pages and controls.
- Presentation must call application use cases rather than repositories directly.
- Infrastructure implements application ports and is composed from the MAUI composition root.
- Formula implementations must expose a stable formula identity and version.
- Formula inputs and outputs must use explicit units and validated types.
- Measurement data must be stored independently from calculated results.
- Historical results must retain the formula identity and version used to produce them.
- The UI must never be the only place where validation happens.
- No network service, account system, analytics SDK, or synchronization mechanism belongs in the MVP.
- Avoid generic repositories, mediator layers, event buses, or abstractions that do not support a current use case.
- Prefer the smallest design that satisfies the current slice and its tests.

## Development workflow

For every implementation slice:

1. Inspect the relevant files and confirm the slice boundary.
2. Write or update a focused failing test for the next behavior.
3. Run the smallest relevant test and verify the failure is meaningful.
4. Implement the minimum production code required by the test.
5. Run the focused test again.
6. Refactor for clarity without changing behavior.
7. Run the full applicable test suite and build.
8. Review the diff for unrelated changes, privacy issues, and architecture violations.
9. Update documentation when a public decision or boundary changes.
10. Commit the completed slice with a focused message when the repository has Git history enabled.

Do not combine unrelated slices merely because their files are nearby. A reviewer must be able to approve or reject one slice without understanding the whole product.

## Testing expectations

Tests must cover behavior rather than implementation details.

- Domain tests must run without Android, MAUI, SQLite, or a network connection.
- Application tests use in-memory repositories or controlled fakes.
- Infrastructure tests use temporary SQLite databases and verify migrations and transactions.
- ViewModel tests verify state transitions, validation, commands, and error translation.
- UI tests cover only the critical user journeys and important responsive states.
- Every formula includes known examples, valid boundaries, invalid inputs, units, precision, and versioning tests.
- A failed test must be investigated before changing production code to make it pass.

Use the repository's configured commands when they exist. The baseline verification commands are:

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

Run Android-specific build or UI verification when the changed slice touches the MAUI app, Android resources, navigation, or platform behavior.

## Domain and calculation rules

- Store `AgeYears` on each measurement so historical calculations preserve the age used.
- Normalize internal anthropometric inputs to metric units and convert only at formula boundaries when required.
- The initial body-fat formula is the male US Navy equation and converts centimeters to inches before calculation.
- The initial BMR formula is the male Mifflin-St Jeor equation.
- TDEE is BMR multiplied by a named activity level and its defined factor.
- Never silently replace an old formula version. Add a new version and preserve historical results.
- Do not present a numerical result when required inputs are invalid or incomplete.

## Persistence and privacy rules

- SQLite lives in the app's private storage.
- Schema changes use explicit, versioned migrations.
- Profile deletion removes the profile, measurements, and calculation results in one transaction after confirmation.
- Dates stored in persistence use UTC; display conversion belongs to Presentation.
- Do not log raw personal measurements or results unless a user explicitly requests diagnostic export.
- Do not add permissions that are not required by the current feature.
- Never commit credentials, signing keys, device data, or personal sample databases.

## UI and accessibility rules

- Keep screens minimal, readable, and responsive across supported Android sizes.
- Show units next to measurement inputs.
- Use actionable validation messages instead of generic failure text.
- Provide loading, empty, success, and recoverable-error states.
- Use touch targets that are comfortable on mobile devices.
- Support larger text without clipping or hiding critical information.
- Keep user-facing copy localizable and avoid embedding business rules in XAML.

## Dependency rules

Before adding a package, explain which current requirement it satisfies and why the platform or standard library is insufficient. Prefer the existing .NET and MAUI capabilities. Pin package versions centrally and remove unused dependencies.

## Communication and handoff

Every implementation report should state:

- what changed;
- which slice or acceptance criteria it addresses;
- tests and builds that were run;
- any known limitation or follow-up that is explicitly in scope for a later slice.

If requirements conflict or a safe implementation requires a product decision, stop at the smallest decision point and ask one focused question. Do not invent external services, user data, credentials, or product scope.

## Definition of done

A change is done only when:

- its behavior is covered by appropriate tests;
- the relevant tests pass;
- the solution builds in the intended configuration;
- dependencies respect the architecture;
- UI changes handle loading, empty, error, accessibility, and responsive states where applicable;
- personal data remains local and protected;
- documentation and acceptance criteria are up to date;
- the diff contains no unrelated work.

