# Profile CSV Transfer Design

> Historical design note: this document describes the original unprotected CSV transfer format. New exports use the protected `.anthropometry` envelope described in `docs/superpowers/specs/2026-09-20-profile-transfer-encryption-design.md`; the plain CSV format remains only as a temporary, explicitly warned legacy import path.

## Goal

Allow a user to export one complete profile as a portable CSV file through the Android Sharesheet and import that file as a new local profile with its complete measurement history and historical calculation results.

## Decisions

- Export and import operate on one profile per file.
- The v1 transfer format is one UTF-8 CSV document with `text/csv` MIME type.
- The document contains a versioned metadata row, one profile row, measurement rows, and calculation-result rows.
- Exported values use canonical units: kilograms, centimeters, and UTC timestamps. Display language, date order, and Metric/Imperial preferences are not exported.
- Decimal values use invariant culture and timestamps use round-trippable UTC ISO 8601 text.
- Import always creates a new profile. Existing profiles are never overwritten or merged, and all imported IDs are regenerated.
- Imported historical calculation results are preserved with their formula identity and version; import does not silently recalculate them.
- The existing four-profile limit applies to imports.
- Import commits the profile, measurements, and results in one SQLite transaction. Any validation or persistence failure leaves the database unchanged.
- Export is available from profile detail. Import is available from the profile list and refreshes the list after a successful import.

## CSV contract

The file uses a stable header and a `record_type` discriminator. The v1 columns are:

```text
record_type,format_version,exported_at_utc,
source_profile_id,source_measurement_id,source_result_id,
name,gender,height_cm,age_years,activity_level,
created_at_utc,updated_at_utc,measured_at_utc,
measurement_type,weight_kg,neck_cm,abdomen_cm,hip_cm,
measurement_gender,measurement_age_years,measurement_activity_level,
calculation_type,value,unit,formula_id,formula_version,calculated_at_utc
```

The `meta` row carries `format_version = 1`, the export timestamp, and the profile source ID. The `profile` row carries profile settings and timestamps. The `measurement` rows carry source measurement IDs and all canonical measurement fields. The `calculation_result` rows carry source measurement IDs, calculation values, units, formula IDs, formula versions, and calculation timestamps.

The serializer must quote commas, quotes, and newlines according to standard CSV rules. The parser must accept UTF-8 with or without a BOM, reject unknown format versions, reject duplicate source IDs and dangling references, and reject invalid enum, decimal, timestamp, or domain values before persistence.

## Architecture

The Application layer owns the transfer document, CSV serializer, export/import use cases, validation, ID remapping, and import summary. A focused `IProfileTransferRepository` port provides a complete profile snapshot and a transactional import operation. Infrastructure implements that port with the existing SQLite connection factory and tables; no schema migration is required.

The Presentation layer owns Android file operations. A MAUI adapter writes exports to `FileSystem.CacheDirectory` and calls `Share.Default.RequestAsync` with `ShareFileRequest`. The same adapter calls `FilePicker.Default.PickAsync`, copies the selected content into a stream, and returns it to the import use case. The app does not request broad storage permissions.

Official platform references:

- [.NET MAUI file sharing](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/data/share?view=net-maui-10.0)
- [.NET MAUI FilePicker](https://learn.microsoft.com/en-us/dotnet/api/microsoft.maui.storage.filepicker.pickasync?view=net-maui-10.0)
- [Android Storage Access Framework](https://developer.android.com/training/data-storage/shared/documents-files)

## User flow and errors

Export shows a short personal-health-data warning before opening the Sharesheet. Import shows a preview containing the profile name, gender, measurement count, and result count before confirmation. Cancellation is a no-op. Invalid, unsupported, incomplete, over-limit, and persistence failures are translated into localized actionable messages. Imported profiles remain local unless the user explicitly shares the exported file.

## Testing

Application tests cover CSV round trips, invariant formatting, escaping, malformed files, unsupported versions, broken references, ID remapping, profile limits, and preservation of formula metadata. Infrastructure tests cover complete transactional commits and rollback on a failed insert. App tests cover command/navigation state and markup. Android smoke testing verifies sharing to an installed target and importing from Downloads or a cloud document provider without storage permissions.

## Out of scope for v1

- Multiple profiles in one file.
- Profile merging or overwrite.
- Encryption or password-protected exports.
- Automatic cloud backup or synchronization.
- JSON, ZIP, PDF, or third-party import formats.
