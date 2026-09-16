# V2 history, correction, chart, and release acceptance

Run this checklist with the same disposable `Synthetic Guidance` profile used
by the guidance workbook. The deterministic sample-data action creates up to 30
days of alternating size-based and weight-only measurements and persists their
calculation results. Do not collect or commit real health data, screenshots, or
an app database.

**Validation status:** User-confirmed complete on 2026-09-16. Detailed session
metrics are intentionally not retained here because they could contain personal
or identifying information.

## History and correction tasks

| ID | Task | Outcome | Notes without personal data |
| --- | --- | --- | --- |
| H1 | Open history and identify both size-based and weight-only rows. | _pending_ | |
| H2 | Explain the warning icon and yellow row without being coached. | _pending_ | |
| H3 | Open results for a weight-only row and return to history. | _pending_ | |
| H4 | Edit one synthetic entry and confirm the changed values remain after reload. | _pending_ | |
| H5 | Delete one synthetic entry and confirm the row is gone after reload. | _pending_ | |
| H6 | Filter history by measurement type and date range. | _pending_ | |
| H7 | Open the filtered metric chart and tap a point to read its date and value. | _pending_ | |

## Release matrix

Run the matrix with network access disabled. Mark each item `pass`, `fail`, or
`not run`, and record only technical observations.

| Area | Check | Result | Notes |
| --- | --- | --- | --- |
| Startup | Cold start initializes the private SQLite database offline. | _not run_ | |
| Persistence | Close and reopen preserves profile, settings, history, and results. | _not run_ | |
| Deletion | Profile/measurement deletion requires confirmation and removes dependent data. | _not run_ | |
| Small screen | Main actions remain reachable on a small supported emulator. | _not run_ | |
| Keyboard | Measurement form scrolls while the keyboard is visible. | _not run_ | |
| Large text | Labels, units, results, and actions do not clip at larger text size. | _not run_ | |
| Light theme | Cards, actions, warning rows, and icons remain readable. | _not run_ | |
| Dark theme | Cards, actions, warning rows, and icons remain readable. | _not run_ | |
| Metric | Height, weight, and sizes use metric display units while persistence remains metric. | _not run_ | |
| Imperial | Height, weight, and sizes use imperial display units while calculations remain metric. | _not run_ | |
| English | Core profile, measurement, history, settings, help, and result copy is understandable. | _not run_ | |
| Spanish | Core profile, measurement, history, settings, help, and result copy is understandable. | _not run_ | |
| German | Core profile, measurement, history, settings, help, and result copy is understandable. | _not run_ | |
| Privacy | No network requests, analytics, unnecessary permissions, credentials, or raw measurement logs. | _not run_ | |

## Automated verification record

The automated checks for this handoff are run from the repository root:

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
dotnet build src/Anthropometry.App/Anthropometry.App.csproj -f net10.0-android -c Release
```

Record the command date, SDK/workload versions, and final pass/fail output in
the release review. The Android matrix and the two human sessions are separate
from automated tests; their completion is recorded above by user confirmation
rather than inferred from automated results.

### Verification recorded on 2026-09-16

- .NET SDK: `10.0.303`.
- MAUI workload: manifest `10.0.20/10.0.100` from SDK `10.0.300`.
- `dotnet restore -m:1`: passed.
- `dotnet build --configuration Release --no-restore`: passed with 0 warnings
  and 0 errors.
- `dotnet test --configuration Release --no-restore`: passed, 241 tests total
  (54 Domain, 44 Application, 13 Infrastructure, 130 App).
- Android Release build: passed with 0 warnings and 0 errors.
- Human sessions and the Android device matrix: completed, per user confirmation.
