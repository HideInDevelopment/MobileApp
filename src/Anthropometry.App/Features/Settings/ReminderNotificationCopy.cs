namespace Anthropometry.App.Features.Settings;

public static class ReminderNotificationCopy
{
    public static (string Title, string Message) For(string? languageCode, ReminderKind kind)
    {
        var language = languageCode?.ToLowerInvariant();
        return kind switch
        {
            ReminderKind.Daily => language switch
            {
                "es" => ("Recordatorio de medición", "Es hora de registrar una nueva medición."),
                "de" => ("Messungserinnerung", "Zeit, eine neue Messung einzutragen."),
                _ => ("Measurement reminder", "It is time to record a new measurement.")
            },
            ReminderKind.Inactivity => language switch
            {
                "es" => ("Te echamos de menos", "Ha pasado un tiempo. Abre la aplicación cuando quieras volver a medirte."),
                "de" => ("Wir vermissen dich", "Es ist eine Weile her. Öffne die App, wenn du wieder messen möchtest."),
                _ => ("We miss you", "It has been a while. Open the app when you are ready to measure again.")
            },
            _ => ("Anthropometry", "Open the app to continue tracking your measurements.")
        };
    }
}

public enum ReminderKind
{
    Daily = 1,
    Inactivity = 2
}
