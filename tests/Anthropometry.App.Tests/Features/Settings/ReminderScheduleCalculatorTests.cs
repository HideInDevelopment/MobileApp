using Anthropometry.App.Features.Settings;

namespace Anthropometry.App.Tests.Features.Settings;

public sealed class ReminderScheduleCalculatorTests
{
    [Fact]
    public void Daily_reminder_uses_the_next_occurrence_of_the_selected_time()
    {
        var before = new DateTimeOffset(2026, 9, 16, 7, 0, 0, TimeSpan.Zero);
        var after = new DateTimeOffset(2026, 9, 16, 9, 0, 0, TimeSpan.Zero);

        Assert.Equal(
            new DateTimeOffset(2026, 9, 16, 8, 30, 0, TimeSpan.Zero),
            ReminderScheduleCalculator.NextDailyUtc(before, new TimeSpan(8, 30, 0), TimeZoneInfo.Utc));
        Assert.Equal(
            new DateTimeOffset(2026, 9, 17, 8, 30, 0, TimeSpan.Zero),
            ReminderScheduleCalculator.NextDailyUtc(after, new TimeSpan(8, 30, 0), TimeZoneInfo.Utc));
    }

    [Fact]
    public void Inactivity_reminder_uses_the_selected_number_of_local_days()
    {
        var lastOpened = new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);

        var result = ReminderScheduleCalculator.InactivityUtc(
            lastOpened,
            14,
            new TimeSpan(8, 30, 0),
            TimeZoneInfo.Utc);

        Assert.Equal(
            new DateTimeOffset(2026, 9, 30, 8, 30, 0, TimeSpan.Zero),
            result);
    }
}
