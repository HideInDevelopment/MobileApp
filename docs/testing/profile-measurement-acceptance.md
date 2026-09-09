# Profile and measurement acceptance checklist

Run this checklist on an Android emulator or device with network access disabled.

- [ ] On first launch with no profiles, only the centered create action is shown; the top `Add profile` action is absent.
- [ ] Creating profiles one through three keeps the top `Add profile` action enabled.
- [ ] Creating the fourth profile leaves `Add profile` visible but disabled.
- [ ] Profile editing saves height, age, and activity level for future measurements.
- [ ] `Add weight` accepts only a weight, saves it locally, and returns to the profile without opening results.
- [ ] `Add measurements` accepts weight, neck, and abdomen and produces body-fat, BMR, and TDEE results.
- [ ] After a size-based measurement, adding a weight-only measurement leaves the previous results unchanged and shows the warning icon on the profile.
- [ ] Adding a later size-based measurement hides the warning icon again.
- [ ] History dates use `dd/MM/yyyy`; measurement labels are friendly; weight-only rows do not offer `View results`.
- [ ] `Settings` and `Help` appear in the top bar and do nothing when selected.
- [ ] Closing and reopening the app preserves profiles, profile settings, measurements, and historical results.

Record the device/API level and any failed item in the release review. Use the Android build and run commands from `README.md` before starting the checklist.
