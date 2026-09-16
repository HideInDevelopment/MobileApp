using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Anthropometry.App.Features.Settings;
using Microsoft.Maui.Storage;

namespace Anthropometry.App;

public sealed class AndroidReminderScheduler : IReminderScheduler
{
    internal const string ReminderKindExtra = "reminder_kind";
    private const int DailyRequestCode = 4101;
    private const int InactivityRequestCode = 4102;

    private readonly IReminderSettingsStore _store;
    private readonly Context _context = global::Android.App.Application.Context;

    public AndroidReminderScheduler(IReminderSettingsStore store)
    {
        _store = store;
    }

    public void Schedule(ReminderSettings settings, DateTimeOffset? lastOpenedAtUtc)
    {
        Cancel();

        if (!settings.IsEnabled)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var localTimeZone = TimeZoneInfo.Local;
        if (settings.DailyEnabled)
        {
            ScheduleAlarm(
                ReminderKind.Daily,
                ReminderScheduleCalculator.NextDailyUtc(now, settings.ReminderTime, localTimeZone));
        }

        if (settings.InactivityEnabled && lastOpenedAtUtc.HasValue)
        {
            var target = ReminderScheduleCalculator.InactivityUtc(
                lastOpenedAtUtc.Value,
                settings.InactivityDays,
                settings.ReminderTime,
                localTimeZone);
            if (target <= now)
            {
                target = ReminderScheduleCalculator.InactivityUtc(
                    now,
                    settings.InactivityDays,
                    settings.ReminderTime,
                    localTimeZone);
            }

            ScheduleAlarm(ReminderKind.Inactivity, target);
        }
    }

    public void Cancel()
    {
        var alarmManager = _context.GetSystemService(Context.AlarmService) as AlarmManager;
        alarmManager?.Cancel(CreatePendingIntent(ReminderKind.Daily));
        alarmManager?.Cancel(CreatePendingIntent(ReminderKind.Inactivity));
    }

    public void RescheduleFromStoredSettings()
        => Schedule(_store.LoadSettings(), _store.LoadLastOpenedAtUtc());

    private void ScheduleAlarm(ReminderKind kind, DateTimeOffset triggerAtUtc)
    {
        var alarmManager = _context.GetSystemService(Context.AlarmService) as AlarmManager;
        if (alarmManager is null)
        {
            return;
        }

        var pendingIntent = CreatePendingIntent(kind);
        var triggerAtMillis = triggerAtUtc.ToUnixTimeMilliseconds();
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
        }
        else
        {
            alarmManager.Set(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
        }
    }

    private PendingIntent CreatePendingIntent(ReminderKind kind)
    {
        var intent = new Intent(_context, typeof(ReminderAlarmReceiver));
        intent.PutExtra(ReminderKindExtra, (int)kind);
        var flags = PendingIntentFlags.UpdateCurrent;
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            flags |= PendingIntentFlags.Immutable;
        }

        return PendingIntent.GetBroadcast(_context, RequestCode(kind), intent, flags)!;
    }

    private static int RequestCode(ReminderKind kind)
        => kind == ReminderKind.Daily ? DailyRequestCode : InactivityRequestCode;
}

[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class ReminderAlarmReceiver : BroadcastReceiver
{
    private const string ChannelId = "measurement-reminders";

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null)
        {
            return;
        }

        var appContext = context!;
        var kind = (ReminderKind)intent.GetIntExtra(AndroidReminderScheduler.ReminderKindExtra, 0);
        if (kind is not ReminderKind.Daily and not ReminderKind.Inactivity)
        {
            return;
        }

        var settingsStore = new PreferencesReminderSettingsStore();
        var settings = settingsStore.LoadSettings();
        if ((kind == ReminderKind.Daily && !settings.DailyEnabled)
            || (kind == ReminderKind.Inactivity && !settings.InactivityEnabled))
        {
            return;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            CreateChannel(appContext);
        }
        var languageCode = Preferences.Default.Get("language", "en");
        var copy = ReminderNotificationCopy.For(languageCode, kind);
        var builder = new NotificationCompat.Builder(appContext, ChannelId)!;
        builder.SetSmallIcon(Android.Resource.Drawable.IcDialogInfo);
        builder.SetContentTitle(copy.Title);
        builder.SetContentText(copy.Message);
        builder.SetAutoCancel(true);
        builder.SetPriority(NotificationCompat.PriorityDefault);
        var notification = builder.Build();
        NotificationManagerCompat.From(appContext)?.Notify((int)kind, notification);

        new AndroidReminderScheduler(settingsStore).Schedule(settings, settingsStore.LoadLastOpenedAtUtc());
    }

    [System.Runtime.Versioning.SupportedOSPlatform("android26.0")]
    private static void CreateChannel(Context context)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            return;
        }

        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        manager?.CreateNotificationChannel(new NotificationChannel(
            ChannelId,
            "Measurement reminders",
            NotificationImportance.Default));
    }
}

[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter([Intent.ActionBootCompleted])]
public sealed class ReminderBootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || !string.Equals(intent?.Action, Intent.ActionBootCompleted, StringComparison.Ordinal))
        {
            return;
        }

        var store = new PreferencesReminderSettingsStore();
        new AndroidReminderScheduler(store).RescheduleFromStoredSettings();
    }
}
