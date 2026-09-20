# Profile Transfer Encryption Design

## Goal

Protect exported anthropometric profiles when they leave the application while preserving the existing offline, cross-device import flow. New exports use the `.anthropometry` extension, conceal the CSV payload, and reject modified files before any profile data reaches SQLite.

## Context and threat model

The existing importer validates the CSV schema, domain values, IDs, relationships, and persistence transaction. That protects against malformed or accidentally corrupted data, but an attacker can still edit a plain CSV and keep the edited values valid.

The protected format must defend against:

- a copied file being read by an unintended recipient;
- a file being modified while stored or shared outside the app;
- malformed or resource-exhausting envelopes reaching the CSV parser or database;
- partial imports after any validation, decryption, or persistence failure.

It does not prevent a person who knows the passphrase from intentionally creating a different valid file. Preventing that would require a trusted signing key outside the offline application and is out of scope.

## Key-management decision

Portable exports use a user-provided passphrase. The application derives a 256-bit content-encryption key for each file with PBKDF2-HMAC-SHA256. The passphrase and derived key exist only in memory for the operation and are never persisted, logged, or placed in the APK.

The v1 protected-transfer parameters are:

| Parameter | Value |
|---|---|
| Key derivation | PBKDF2-HMAC-SHA256 |
| PBKDF2 iterations | 600,000 |
| Derived key | 32 bytes |
| Salt | 16 cryptographically random bytes per file |
| Encryption | AES-256-GCM |
| Nonce | 12 cryptographically random bytes per file |
| Authentication tag | 16 bytes |
| Maximum protected file size | 10 MiB |
| Minimum passphrase | 12 non-whitespace characters |

PBKDF2 is used instead of adding an Argon2 package because .NET provides PBKDF2 in the base cryptography APIs and the repository currently avoids dependencies without a concrete requirement. The iteration count is stored in the envelope and bounded during parsing so a future version can increase it without accepting attacker-controlled unbounded work.

Android Keystore is not used for the portable file key. It remains suitable for device-local secrets, but a Keystore key cannot be assumed to be available on another device. The portable secret is therefore the user’s passphrase.

## `.anthropometry` envelope

The file is a compact binary envelope, not a readable CSV. Its payload is the existing versioned UTF-8 CSV document. The envelope has this ordered structure:

```text
magic                 ASCII `ANTHROPOMETRY\0`
envelope_version      unsigned byte, currently 1
payload_version       unsigned byte, current CSV format version 1
kdf_id                unsigned byte, PBKDF2-HMAC-SHA256
cipher_id             unsigned byte, AES-256-GCM
iteration_count       unsigned 32-bit big-endian integer
salt                  16 bytes
nonce                 12 bytes
authentication_tag    16 bytes
ciphertext            remaining bytes
```

The header through the nonce is authenticated as AES-GCM associated data. Changes to algorithms, versions, iterations, salt, nonce, tag, or ciphertext therefore fail authentication. All lengths and iteration bounds are checked before allocation or key derivation.

The application uses the existing `ProfileTransferCsvSerializer` only after successful decryption. The decrypted CSV remains canonical: kilograms, centimetres, invariant decimals, and UTC timestamps.

## User flow

### Export

1. Show the existing personal-health-data warning.
2. Ask for a passphrase and confirmation in an obscured password input.
3. Enforce the minimum passphrase rule.
4. Serialize the complete profile to the existing CSV payload.
5. Encrypt the payload and share `anthropometry-<safe-name>-<yyyyMMdd>.anthropometry`.

Cancelling the warning or passphrase flow performs no export. The passphrase is not remembered.

### Import

1. Pick `.anthropometry` files, while also accepting `.csv` only as a temporary legacy path.
2. Detect the envelope by its magic bytes, not by filename.
3. Ask for the passphrase when the protected envelope is detected.
4. Decrypt and authenticate the bytes.
5. Parse and validate the inner CSV completely.
6. Show the existing preview and confirmation, including whether the file is an unprotected legacy CSV.
7. Import the complete graph in the existing SQLite transaction.

Wrong passwords, modified files, unsupported envelope versions, invalid legacy CSV, oversized files, and cancellation are recoverable errors. None of them writes data.

Legacy CSV imports are supported temporarily so profiles exported by the previous release are not stranded. They are explicitly labeled unprotected and are never produced by the new exporter. A later cleanup slice may remove legacy support after the migration window.

## Architecture

The Application layer owns the envelope format, cryptography, passphrase validation, protected/legacy detection, and error mapping. It uses only BCL cryptography APIs and remains independent of Android, MAUI, SQLite, and XAML.

The existing export/import use cases continue to own profile snapshotting, CSV serialization, validation, ID remapping, profile limits, and transactional persistence. They receive passphrases as operation inputs and never persist them.

The Presentation layer owns password prompts and localized copy. The MAUI file adapter returns the selected filename and bounded bytes, accepts `.anthropometry` and legacy `.csv`, and continues using the private cache directory, Sharesheet, and Storage Access Framework without broad storage permissions.

## Error and privacy rules

- Never log passphrases, derived keys, plaintext CSV, ciphertext, or raw profile data.
- Report authentication failure with one generic message such as “The password is incorrect or the file was modified.”
- Do not reveal whether a failure came from a wrong password or a tampered file.
- Zero temporary key and password-byte buffers with `CryptographicOperations.ZeroMemory` when possible.
- Do not store a recovery key or silently upload a backup.
- Keep existing domain validation and transaction boundaries; encryption does not replace validation.

## Testing

Application tests must cover protected round trips, random salt/nonce output, wrong passphrases, ciphertext/tag/header tampering, unsupported versions/algorithms, iteration bounds, oversized input, passphrase validation, legacy CSV detection, new `.anthropometry` filenames, and preservation of the existing CSV/profile behavior after decryption.

Presentation tests must cover export/import passphrase cancellation, confirmation mismatch, protected-file retry, legacy warning, localized messages, and `.anthropometry` picker filters. Android build verification must compile the MAUI adapter without new permissions or packages.

Manual Android smoke testing must cover export to the Sharesheet, import from Downloads or another document provider, wrong password, modified file, legacy CSV warning, cancellation, and successful profile/history restoration.

## Out of scope

- Android Keystore as the portable export key.
- Password recovery, account-based key escrow, or cloud synchronization.
- Digital signatures that authenticate a file against a developer or server identity.
- Remembering passphrases in Preferences or SecureStorage.
- Argon2 package adoption in this slice.
- Multiple profiles per file, merging, or overwriting existing profiles.
