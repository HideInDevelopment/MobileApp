using Anthropometry.Application.Common;
using Anthropometry.Domain.Measurements;

namespace Anthropometry.App.Features.Measurements;

public sealed record MeasurementHistoryItem(
    MeasurementDto Measurement,
    string DateText,
    string MeasurementTypeText)
{
    public bool CanViewResults => Measurement.Type is MeasurementType.WeightOnly or MeasurementType.WeightAndSizes;
}
