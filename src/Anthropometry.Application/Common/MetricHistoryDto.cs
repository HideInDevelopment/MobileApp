using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;

namespace Anthropometry.Application.Common;

public sealed record MetricHistoryDto(
    MeasurementId MeasurementId,
    DateTimeOffset MeasuredAtUtc,
    CalculationType? CalculationType,
    decimal Value,
    string Unit);
