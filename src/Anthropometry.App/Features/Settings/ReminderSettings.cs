namespace Anthropometry.App.Features.Settings;

public sealed record ReminderSettings(
    bool DailyEnabled,
    TimeSpan ReminderTime,
    bool InactivityEnabled,
    int InactivityDays)
{
    public static ReminderSettings Default { get; } = new(false, new TimeSpan(9, 0, 0), false, 7);

    public bool IsEnabled => DailyEnabled || InactivityEnabled;

    public bool IsValid =>
        ReminderTime >= TimeSpan.Zero
        && ReminderTime < TimeSpan.FromDays(1)
        && InactivityDays is 7 or 14;
}

public sealed record ReminderIntervalOption(int Days, string DisplayName);

public interface IReminderSettingsStore
{
    ReminderSettings LoadSettings();

    void SaveSettings(ReminderSettings settings);

    DateTimeOffset? LoadLastOpenedAtUtc();

    void SaveLastOpenedAtUtc(DateTimeOffset openedAtUtc);
}

public interface IReminderScheduler
{
    void Schedule(ReminderSettings settings, DateTimeOffset? lastOpenedAtUtc);

    void Cancel();
}

public interface IReminderPermissionService
{
    Task<bool> EnsureGrantedAsync(CancellationToken cancellationToken = default);
}

public sealed class ReminderCoordinator
{
    private readonly IReminderSettingsStore _store;
    private readonly IReminderScheduler _scheduler;
    private readonly IReminderPermissionService _permission;
    private ReminderSettings _current = ReminderSettings.Default;
    private DateTimeOffset? _lastOpenedAtUtc;

    public ReminderCoordinator(
        IReminderSettingsStore store,
        IReminderScheduler scheduler,
        IReminderPermissionService permission)
    {
        _store = store;
        _scheduler = scheduler;
        _permission = permission;
    }

    public ReminderSettings Current => _current;

    public DateTimeOffset? LastOpenedAtUtc => _lastOpenedAtUtc;

    public void Initialize()
    {
        _current = _store.LoadSettings();
        _lastOpenedAtUtc = _store.LoadLastOpenedAtUtc();

        if (_current.IsEnabled)
        {
            _scheduler.Schedule(_current, _lastOpenedAtUtc);
        }
    }

    public async Task<bool> UpdateAsync(
        ReminderSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (!settings.IsValid)
        {
            return false;
        }

        if (settings.IsEnabled)
        {
            if (!await _permission.EnsureGrantedAsync(cancellationToken))
            {
                return false;
            }
        }

        ApplySettings(settings);

        return true;
    }

    public void ApplySettings(ReminderSettings settings)
    {
        if (!settings.IsValid)
        {
            return;
        }

        _current = settings;
        _store.SaveSettings(settings);

        if (settings.IsEnabled)
        {
            _scheduler.Schedule(settings, _lastOpenedAtUtc);
        }
        else
        {
            _scheduler.Cancel();
        }
    }

    public void MarkAppOpened(DateTimeOffset openedAtUtc)
    {
        _lastOpenedAtUtc = openedAtUtc.ToUniversalTime();
        _store.SaveLastOpenedAtUtc(_lastOpenedAtUtc.Value);

        if (_current.IsEnabled)
        {
            _scheduler.Schedule(_current, _lastOpenedAtUtc);
        }
    }
}
