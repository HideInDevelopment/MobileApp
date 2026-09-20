using Anthropometry.Application.Common;
using Anthropometry.Domain.Measurements;

namespace Anthropometry.App.Features.Measurements;

public sealed record MeasurementHistoryItem(
    MeasurementDto Measurement,
    string DateText,
    string MeasurementTypeText,
    string WeightText,
    string HeightText)
{
    public bool CanViewResults => Measurement.Type is MeasurementType.WeightOnly or MeasurementType.WeightAndSizes;

    public bool ShowWarningIcon => Measurement.Type == MeasurementType.WeightOnly;
}

public sealed record MeasurementTypeFilterOption(MeasurementType? Value, string DisplayName);
