# Android acceptance checklist

Run this checklist on an Android emulator or device with network access disabled.

- [ ] The app starts and initializes its private SQLite database.
- [ ] A profile can be created, renamed, and deleted after confirmation.
- [ ] Profile deletion removes its measurements and calculation results.
- [ ] Blank, non-numeric, zero, and negative measurement values show actionable validation.
- [ ] A valid metric measurement produces body-fat, BMR, and TDEE results.
- [ ] Results show friendly titles, values rounded to two decimals, and units only.
- [ ] Closing and reopening the app preserves profiles and measurement history.
- [ ] Empty, loading, success, and recoverable-error states are understandable.
- [ ] The measurement form scrolls with the keyboard visible.
- [ ] Primary actions remain reachable on small phone widths.
- [ ] Larger text does not clip units, results, or actions.
- [ ] No unnecessary permissions, network requests, analytics, credentials, or sample personal data are present.

Use the Android build command from the repository README before starting this checklist. Record the device/API level and any failed item in the release review.
