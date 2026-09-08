using System.Globalization;

namespace Anthropometry.Infrastructure.Persistence.Sqlite;

internal static class SqliteValueConverter
{
    public static string ToUtcString(DateTimeOffset value)
        => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    public static DateTimeOffset ToUtcDateTimeOffset(string value)
        => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime();
}
