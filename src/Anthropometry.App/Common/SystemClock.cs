using Anthropometry.Application.Abstractions;

namespace Anthropometry.App.Common;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
