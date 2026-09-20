using Anthropometry.Application.Common;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Profiles;

public sealed record ProfileTransferSnapshot(
    Profile Profile,
    IReadOnlyList<Measurement> Measurements,
    IReadOnlyList<CalculationResult> Results);

public sealed record ProfileTransferDocument(
    int FormatVersion,
    DateTimeOffset ExportedAtUtc,
    ProfileTransferProfile Profile,
    IReadOnlyList<ProfileTransferMeasurement> Measurements,
    IReadOnlyList<ProfileTransferCalculationResult> Results);

public sealed record ProfileTransferProfile(
    Guid SourceProfileId,
    string Name,
    ProfileGender Gender,
    decimal? HeightCm,
    int? AgeYears,
    ActivityLevel? ActivityLevel,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ProfileTransferMeasurement(
    Guid SourceProfileId,
    Guid SourceMeasurementId,
    MeasurementType Type,
    DateTimeOffset MeasuredAtUtc,
    decimal WeightKg,
    decimal HeightCm,
    decimal? NeckCm,
    decimal? AbdomenCm,
    decimal? HipCm,
    ProfileGender Gender,
    int AgeYears,
    ActivityLevel ActivityLevel);

public sealed record ProfileTransferCalculationResult(
    Guid SourceResultId,
    Guid SourceMeasurementId,
    CalculationType CalculationType,
    decimal Value,
    string Unit,
    string FormulaId,
    string FormulaVersion,
    DateTimeOffset CalculatedAtUtc);

public sealed record ProfileExportFile(string FileName, byte[] Content);

public sealed record ProfileImportPreview(
    ProfileTransferDocument Document,
    string Name,
    ProfileGender Gender,
    int MeasurementCount,
    int CalculationResultCount,
    bool IsLegacyUnprotected = false);

public sealed record ProfileTransferProtectedPayload(byte[] CsvContent, bool IsLegacyUnprotected);

public sealed record ImportedProfile(
    ProfileDto Profile,
    int MeasurementCount,
    int CalculationResultCount);
