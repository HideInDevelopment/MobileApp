using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Measurements;

public sealed record MeasurementHistoryQuery(
    ProfileId ProfileId,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    MeasurementType? Type);
