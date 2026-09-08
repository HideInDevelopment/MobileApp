namespace Anthropometry.Domain.Common;

public static class Guard
{
    public static DomainError? Required(string? value, string code, string messageKey, int maxLength = 100)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new DomainError(code, messageKey);
        }

        return value.Trim().Length > maxLength
            ? new DomainError($"{code}.tooLong", $"{messageKey}.tooLong")
            : null;
    }

    public static DomainError? Positive(decimal value, string code, string messageKey)
        => value > 0 ? null : new DomainError(code, messageKey);

    public static DomainError? Positive(int value, string code, string messageKey)
        => value > 0 ? null : new DomainError(code, messageKey);

    public static DomainError? Utc(DateTimeOffset value, string code, string messageKey)
        => value.Offset == TimeSpan.Zero ? null : new DomainError(code, messageKey);
}
