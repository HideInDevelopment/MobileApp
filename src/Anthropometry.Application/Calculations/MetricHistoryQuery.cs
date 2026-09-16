using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Calculations;

public sealed record MetricHistoryQuery(
    ProfileId ProfileId,
    CalculationType? CalculationType,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc);
