using Anthropometry.App.Display;
using Anthropometry.App.Features.Settings;
using Anthropometry.App.Localization;
using Anthropometry.App.Theme;
using Anthropometry.App.Tests.Support;

namespace Anthropometry.App.Tests.Features.Settings;

public sealed class ReminderSettingsViewModelTests
{
    [Fact]
    public async Task Enabling_daily_reminders_persists_the_selected_time_and_interval()
    {
        var store = new TestReminderSettingsStore();
        var scheduler = new TestReminderScheduler();
        var permission = new TestReminderPermission { IsGranted = true };
        var coordinator = new ReminderCoordinator(store, scheduler, permission);
        coordinator.Initialize();
        var viewModel = new SettingsViewModel(
            TestData.LanguageService(),
            CreateThemeService(),
            TestData.DisplayPreferences(),
            coordinator);

        viewModel.ReminderTime = new TimeSpan(7, 45, 0);
        viewModel.SelectedInactivityInterval = viewModel.InactivityIntervals.Single(option => option.Days == 14);

        var result = await viewModel.SetDailyReminderEnabledAsync(true);

        Assert.True(result);
        Assert.Equal(new TimeSpan(7, 45, 0), store.Settings.ReminderTime);
        Assert.Equal(14, store.Settings.InactivityDays);
        Assert.True(store.Settings.DailyEnabled);
    }

    private static ThemeService CreateThemeService()
    {
        var service = new ThemeService(new TestThemePreferenceStore());
        service.Initialize();
        return service;
    }

    private sealed class TestReminderSettingsStore : IReminderSettingsStore
    {
        public ReminderSettings Settings { get; private set; } = ReminderSettings.Default;

        public DateTimeOffset? LastOpenedAtUtc { get; private set; }

        public ReminderSettings LoadSettings() => Settings;

        public void SaveSettings(ReminderSettings settings) => Settings = settings;

        public DateTimeOffset? LoadLastOpenedAtUtc() => LastOpenedAtUtc;

        public void SaveLastOpenedAtUtc(DateTimeOffset openedAtUtc) => LastOpenedAtUtc = openedAtUtc;
    }

    private sealed class TestReminderScheduler : IReminderScheduler
    {
        public void Schedule(ReminderSettings settings, DateTimeOffset? lastOpenedAtUtc) { }

        public void Cancel() { }
    }

    private sealed class TestReminderPermission : IReminderPermissionService
    {
        public bool IsGranted { get; init; }

        public Task<bool> EnsureGrantedAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(IsGranted);
    }

    private sealed class TestThemePreferenceStore : IThemePreferenceStore
    {
        public string? GetThemeCode() => null;

        public void SetThemeCode(string code) { }
    }
}
