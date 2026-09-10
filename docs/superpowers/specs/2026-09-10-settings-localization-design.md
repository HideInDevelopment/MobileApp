# Settings and Localization Design

## Goal

Add a local Settings screen where users can select English, Spanish, or German, persist that choice, and reopen the app in the selected language.

## Decisions

- English is the default when no valid preference exists.
- Selecting a language applies it immediately and writes the language code to local MAUI `Preferences`.
- App startup reads the saved language before the first feature page is created.
- Translation resources use the standard .NET `.resx` format: English base resources plus `es` and `de` satellite resources.
- Existing visible labels, titles, buttons, validation messages, errors, result names, activity-level names, and accessibility descriptions are localized.
- The settings and language implementation belongs to Presentation. Domain and Application remain unchanged.
- Settings and Help toolbar actions use SVG icons with accessible names. Help remains a placeholder with no behavior.

## Runtime behavior

`LanguageService` owns supported language codes, culture selection, resource lookup, and the language-changed notification. A small preference-store port keeps it testable without MAUI. The MAUI composition root supplies a `Preferences`-backed implementation.

The application applies translated resource values to the application resource dictionary. Pages bind static copy through `DynamicResource`; ViewModels use the same service for computed display text and refresh their collections when the language changes. This avoids rebuilding the navigation stack and keeps the current screen usable after a selection.

## Testing

- Language service tests cover default English, saved-language restoration, persistence on change, supported-language validation, and translated lookup.
- Settings ViewModel tests cover the three options, selected-language restoration, and applying a new language.
- Profile-list navigation tests cover the Settings command.
- Existing App tests continue to verify current behavior while localized expected text is asserted through the service.
