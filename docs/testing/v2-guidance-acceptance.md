# V2 guidance and first-result acceptance

This workbook validates the main V2 flow with synthetic data only. It is intended
for two local sessions:

- **Novice session:** a person who does not routinely use anthropometric formulas.
- **Experienced session:** a person who understands body measurements or energy
  estimates.

Do not enter real health measurements, names, screenshots, or device database
files. Use the disposable profile name `Synthetic Guidance` and the sample-data
action after the first complete measurement.

## Preparation

1. Build and install the Android app using the commands in the repository
   [README](../../README.md).
2. Disable network access on the emulator or device.
3. Start with an empty app database, or remove the disposable test profile
   before each session.
4. Create one profile with synthetic values such as age `35`, height `1.75 m`
   (or the equivalent selected display unit), and a middle activity level.
5. Add one complete measurement with clearly fictional values, then use
   **Generate sample data** to create the deterministic 30-day history.

## Uncoached task script

Read each task exactly as written. Do not explain where to tap or what a result
means until the participant asks for help. Record observations in the table and
use only `complete`, `blocked`, or `uncertain` as the outcome.

| ID | Task | Outcome | Notes without personal data |
| --- | --- | --- | --- |
| G1 | Create a profile and choose an activity level. | _pending_ | |
| G2 | Find help for entering height, weight, and body measurements. | _pending_ | |
| G3 | Record a complete measurement and reach the results. | _pending_ | |
| G4 | Explain in your own words what the body-fat, BMR, and TDEE values represent. | _pending_ | |
| G5 | Identify whether the displayed results are a diagnosis or an estimate. | _pending_ | |
| G6 | Return to the profile and open measurement history. | _pending_ | |

Record the time from the first profile action to the first result, the help
topics opened, and any wrong or uncertain input. Do not record the entered
measurements.

## Acceptance expectations

- The first result is reachable without external explanation.
- Activity-level examples are understandable enough for the participant to
  choose an option.
- Measurement guidance identifies the relevant body location and consistent
  tape placement.
- Result explanations and the disclaimer make clear that values are estimates,
  not medical diagnoses or medical advice.
- An experienced participant can complete the same flow without an unnecessary
  tutorial or forced modal step.
- The user can return to history after viewing results.

## Session record

| Session | Audience | First result time | Help topics opened | Blockers | Follow-up |
| --- | --- | --- | --- | --- | --- |
| S1 | Novice | _pending_ | _pending_ | _pending_ | _pending_ |
| S2 | Experienced | _pending_ | _pending_ | _pending_ | _pending_ |

Human sessions remain a release prerequisite. Until S1 and S2 are completed,
this document is a ready-to-run protocol rather than evidence of user
validation.
