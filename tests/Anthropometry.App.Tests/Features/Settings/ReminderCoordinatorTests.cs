using Anthropometry.App.Features.Settings;

namespace Anthropometry.App.Tests.Features.Settings;

public sealed class ReminderCoordinatorTests
{
    [Fact]
    public async Task Enabling_daily_reminders_persists_settings_and_schedules_them()
    {
        var store = new InMemoryReminderSettingsStore();
        var scheduler = new FakeReminderScheduler();
        var permission = new FakeReminderPermissionService { IsGranted = true };
        var coordinator = new ReminderCoordinator(store, scheduler, permission);
        coordinator.Initialize();

        var settings = new ReminderSettings(
            true,
            new TimeSpan(8, 30, 0),
            false,
            7);

        var result = await coordinator.UpdateAsync(settings);

        Assert.True(result);
        Assert.Equal(settings, store.Settings);
        Assert.Equal(settings, scheduler.Settings);
        Assert.Equal(1, permission.RequestCount);
    }

    [Fact]
    public async Task Denied_notification_permission_keeps_reminders_disabled()
    {
        var store = new InMemoryReminderSettingsStore();
        var scheduler = new FakeReminderScheduler();
        var permission = new FakeReminderPermissionService { IsGranted = false };
        var coordinator = new ReminderCoordinator(store, scheduler, permission);
        coordinator.Initialize();

        var result = await coordinator.UpdateAsync(
            new ReminderSettings(true, new TimeSpan(9, 0, 0), false, 7));

        Assert.False(result);
        Assert.Equal(ReminderSettings.Default, coordinator.Current);
        Assert.Null(store.Settings);
        Assert.Null(scheduler.Settings);
    }

    [Fact]
    public async Task Opening_the_app_resets_the_inactivity_reference_and_reschedules()
    {
        var store = new InMemoryReminderSettingsStore
        {
            Settings = new ReminderSettings(false, new TimeSpan(9, 0, 0), true, 14)
        };
        var scheduler = new FakeReminderScheduler();
        var permission = new FakeReminderPermissionService { IsGranted = true };
        var coordinator = new ReminderCoordinator(store, scheduler, permission);
        coordinator.Initialize();

        var openedAt = new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);
        coordinator.MarkAppOpened(openedAt);

        Assert.Equal(openedAt, store.LastOpenedAtUtc);
        Assert.Equal(openedAt, scheduler.LastOpenedAtUtc);
        Assert.Equal(store.Settings, scheduler.Settings);
    }

    [Fact]
    public async Task Reenabling_reminders_rechecks_permission_after_it_was_revoked()
    {
        var store = new InMemoryReminderSettingsStore();
        var scheduler = new FakeReminderScheduler();
        var permission = new FakeReminderPermissionService { IsGranted = true };
        var coordinator = new ReminderCoordinator(store, scheduler, permission);

        var enabled = new ReminderSettings(true, new TimeSpan(9, 0, 0), false, 7);
        Assert.True(await coordinator.UpdateAsync(enabled));

        Assert.True(await coordinator.UpdateAsync(ReminderSettings.Default));
        permission.IsGranted = false;

        var result = await coordinator.UpdateAsync(enabled);

        Assert.False(result);
        Assert.Equal(ReminderSettings.Default, coordinator.Current);
        Assert.Equal(2, permission.RequestCount);
    }

    private sealed class InMemoryReminderSettingsStore : IReminderSettingsStore
    {
        public ReminderSettings? Settings { get; set; }

        public DateTimeOffset? LastOpenedAtUtc { get; set; }

        public ReminderSettings LoadSettings() => Settings ?? ReminderSettings.Default;

        public void SaveSettings(ReminderSettings settings) => Settings = settings;

        public DateTimeOffset? LoadLastOpenedAtUtc() => LastOpenedAtUtc;

        public void SaveLastOpenedAtUtc(DateTimeOffset openedAtUtc) => LastOpenedAtUtc = openedAtUtc;
    }

    private sealed class FakeReminderScheduler : IReminderScheduler
    {
        public ReminderSettings? Settings { get; private set; }

        public DateTimeOffset? LastOpenedAtUtc { get; private set; }

        public void Schedule(ReminderSettings settings, DateTimeOffset? lastOpenedAtUtc)
        {
            Settings = settings;
            LastOpenedAtUtc = lastOpenedAtUtc;
        }

        public void Cancel()
        {
            Settings = null;
            LastOpenedAtUtc = null;
        }
    }

    private sealed class FakeReminderPermissionService : IReminderPermissionService
    {
        public bool IsGranted { get; set; }

        public int RequestCount { get; private set; }

        public Task<bool> EnsureGrantedAsync(CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(IsGranted);
        }
    }
}
