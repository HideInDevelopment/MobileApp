using System.Globalization;
using Microsoft.Maui.Storage;

namespace Anthropometry.App.Features.Settings;

public sealed class PreferencesReminderSettingsStore : IReminderSettingsStore
{
    private const string DailyEnabledKey = "reminders.daily.enabled";
    private const string ReminderTimeKey = "reminders.time";
    private const string InactivityEnabledKey = "reminders.inactivity.enabled";
    private const string InactivityDaysKey = "reminders.inactivity.days";
    private const string LastOpenedAtUtcKey = "reminders.last-opened-utc";

    public ReminderSettings LoadSettings()
    {
        var time = TimeSpan.TryParseExact(
            Preferences.Default.Get(ReminderTimeKey, "09:00"),
            "hh\\:mm",
            CultureInfo.InvariantCulture,
            out var parsedTime)
            ? parsedTime
            : ReminderSettings.Default.ReminderTime;
        var days = Preferences.Default.Get(InactivityDaysKey, ReminderSettings.Default.InactivityDays);

        return new ReminderSettings(
            Preferences.Default.Get(DailyEnabledKey, false),
            time,
            Preferences.Default.Get(InactivityEnabledKey, false),
            days is 7 or 14 ? days : ReminderSettings.Default.InactivityDays);
    }

    public void SaveSettings(ReminderSettings settings)
    {
        Preferences.Default.Set(DailyEnabledKey, settings.DailyEnabled);
        Preferences.Default.Set(ReminderTimeKey, settings.ReminderTime.ToString("hh\\:mm", CultureInfo.InvariantCulture));
        Preferences.Default.Set(InactivityEnabledKey, settings.InactivityEnabled);
        Preferences.Default.Set(InactivityDaysKey, settings.InactivityDays);
    }

    public DateTimeOffset? LoadLastOpenedAtUtc()
    {
        var value = Preferences.Default.Get(LastOpenedAtUtcKey, string.Empty);
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed.ToUniversalTime()
            : null;
    }

    public void SaveLastOpenedAtUtc(DateTimeOffset openedAtUtc)
        => Preferences.Default.Set(LastOpenedAtUtcKey, openedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
}
