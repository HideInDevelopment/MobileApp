namespace Anthropometry.App.Tests.Features.Settings;

public sealed class ReminderSettingsMarkupTests
{
    [Fact]
    public void Exposes_daily_time_and_inactivity_reminder_controls()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Features", "Settings", "SettingsPage.xaml"));
        var markup = File.ReadAllText(path);

        Assert.Contains("DailyReminder", markup);
        Assert.Contains("ReminderTime", markup);
        Assert.Contains("InactivityReminder", markup);
        Assert.Contains("SelectedInactivityInterval", markup);
        Assert.Contains("OnInactivityIntervalSelectorClicked", markup);
    }
}
