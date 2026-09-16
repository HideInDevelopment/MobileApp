namespace Anthropometry.App.Features.Settings;

public static class ReminderScheduleCalculator
{
    public static DateTimeOffset NextDailyUtc(
        DateTimeOffset nowUtc,
        TimeSpan reminderTime,
        TimeZoneInfo timeZone)
    {
        var localNow = TimeZoneInfo.ConvertTime(nowUtc, timeZone);
        var localDate = localNow.Date;
        var candidate = localDate.Add(reminderTime);
        if (candidate <= localNow.DateTime)
        {
            candidate = candidate.AddDays(1);
        }

        return ToUtc(candidate, timeZone);
    }

    public static DateTimeOffset InactivityUtc(
        DateTimeOffset lastOpenedAtUtc,
        int inactivityDays,
        TimeSpan reminderTime,
        TimeZoneInfo timeZone)
    {
        var localLastOpened = TimeZoneInfo.ConvertTime(lastOpenedAtUtc, timeZone);
        var candidate = localLastOpened.Date.AddDays(inactivityDays).Add(reminderTime);
        return ToUtc(candidate, timeZone);
    }

    private static DateTimeOffset ToUtc(DateTime localDateTime, TimeZoneInfo timeZone)
    {
        if (timeZone.IsInvalidTime(localDateTime))
        {
            localDateTime = localDateTime.AddHours(1);
        }

        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone));
    }
}
