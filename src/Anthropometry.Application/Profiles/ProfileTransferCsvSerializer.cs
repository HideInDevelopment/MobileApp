using System.Globalization;
using System.Text;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Common;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Profiles;

public static class ProfileTransferCsvSerializer
{
    public const int CurrentFormatVersion = 1;

    private static readonly string[] Columns =
    [
        "record_type", "format_version", "exported_at_utc", "source_profile_id", "source_measurement_id",
        "source_result_id", "name", "gender", "height_cm", "age_years", "activity_level",
        "created_at_utc", "updated_at_utc", "measured_at_utc", "measurement_type", "weight_kg",
        "neck_cm", "abdomen_cm", "hip_cm", "measurement_gender", "measurement_age_years",
        "measurement_activity_level", "calculation_type", "value", "unit", "formula_id",
        "formula_version", "calculated_at_utc"
    ];

    public static byte[] Serialize(ProfileTransferSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var exportedAtUtc = DateTimeOffset.UtcNow;
        var builder = new StringBuilder();
        AppendRow(builder, Columns);
        AppendRow(builder, Row(
            "meta", CurrentFormatVersion, FormatTimestamp(exportedAtUtc)));

        var profile = snapshot.Profile;
        AppendRow(builder, Row(
            "profile",
            sourceProfileId: profile.Id.Value.ToString("D"),
            name: profile.Name,
            gender: profile.Gender.ToString(),
            heightCm: profile.Settings?.HeightCm,
            ageYears: profile.Settings?.AgeYears,
            activityLevel: profile.Settings?.ActivityLevel.ToString(),
            createdAtUtc: FormatTimestamp(profile.CreatedAtUtc),
            updatedAtUtc: FormatTimestamp(profile.UpdatedAtUtc)));

        foreach (var measurement in snapshot.Measurements)
        {
            AppendRow(builder, Row(
                "measurement",
                sourceProfileId: measurement.ProfileId.Value.ToString("D"),
                sourceMeasurementId: measurement.Id.Value.ToString("D"),
                measuredAtUtc: FormatTimestamp(measurement.MeasuredAtUtc),
                measurementType: measurement.Type.ToString(),
                weightKg: measurement.WeightKg,
                heightCm: measurement.HeightCm,
                neckCm: measurement.NeckCm,
                abdomenCm: measurement.AbdomenCm,
                hipCm: measurement.HipCm,
                measurementGender: measurement.Gender.ToString(),
                measurementAgeYears: measurement.AgeYears,
                measurementActivityLevel: measurement.ActivityLevel.ToString()));
        }

        foreach (var result in snapshot.Results)
        {
            AppendRow(builder, Row(
                "calculation_result",
                sourceMeasurementId: result.MeasurementId.Value.ToString("D"),
                sourceResultId: result.Id.Value.ToString("D"),
                calculationType: result.CalculationType.ToString(),
                value: result.Value,
                unit: result.Unit,
                formulaId: result.FormulaId,
                formulaVersion: result.FormulaVersion,
                calculatedAtUtc: FormatTimestamp(result.CalculatedAtUtc)));
        }

        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(builder.ToString());
    }

    public static Result<ProfileTransferDocument> Parse(Stream content)
    {
        if (content is null)
        {
            return Failure("profileTransfer.content.required", "Errors.ProfileTransferFileInvalid");
        }

        try
        {
            using var reader = new StreamReader(content, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var text = reader.ReadToEnd();
            var rows = ParseRows(text);
            if (rows.Count == 0 || rows[0].Count != Columns.Length || !rows[0].SequenceEqual(Columns, StringComparer.Ordinal))
            {
                return Failure("profileTransfer.header.invalid", "Errors.ProfileTransferFileInvalid");
            }

            if (rows.Skip(1).Any(row => row.Count != Columns.Length
                || row[0] is not ("meta" or "profile" or "measurement" or "calculation_result")))
            {
                return Failure("profileTransfer.record.invalid", "Errors.ProfileTransferFileInvalid");
            }

            return ParseDocument(rows.Skip(1).ToArray());
        }
        catch (DecoderFallbackException)
        {
            return Failure("profileTransfer.encoding.invalid", "Errors.ProfileTransferFileInvalid");
        }
        catch (FormatException)
        {
            return Failure("profileTransfer.field.invalid", "Errors.ProfileTransferFileInvalid");
        }
    }

    private static Result<ProfileTransferDocument> ParseDocument(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var metaRows = RowsOfType(rows, "meta");
        var profileRows = RowsOfType(rows, "profile");
        if (metaRows.Count != 1 || profileRows.Count != 1)
        {
            return Failure("profileTransfer.shape.invalid", "Errors.ProfileTransferFileInvalid");
        }

        var meta = metaRows[0];
        if (!TryInt(meta, "format_version", out var version) || version != CurrentFormatVersion)
        {
            return Failure("profileTransfer.formatVersion.unsupported", "Errors.ProfileTransferFormatUnsupported");
        }

        if (!TryUtcTimestamp(meta, "exported_at_utc", out var exportedAtUtc))
        {
            return Failure("profileTransfer.field.invalid", "Errors.ProfileTransferFileInvalid");
        }

        var profileRow = profileRows[0];
        if (!TryGuid(profileRow, "source_profile_id", out var sourceProfileId)
            || !TryRequired(profileRow, "name", out var name)
            || !TryEnum(profileRow, "gender", out ProfileGender gender)
            || !TryUtcTimestamp(profileRow, "created_at_utc", out var createdAtUtc)
            || !TryUtcTimestamp(profileRow, "updated_at_utc", out var updatedAtUtc))
        {
            return Failure("profileTransfer.field.required", "Errors.ProfileTransferFileInvalid");
        }

        decimal? heightCm = null;
        int? ageYears = null;
        ActivityLevel? activityLevel = null;
        var settingsValues = new[]
        {
            profileRow[ColumnIndex("height_cm")],
            profileRow[ColumnIndex("age_years")],
            profileRow[ColumnIndex("activity_level")]
        };
        if (settingsValues.Any(value => !string.IsNullOrWhiteSpace(value)))
        {
            if (!TryDecimal(profileRow, "height_cm", out var parsedHeight)
                || !TryInt(profileRow, "age_years", out var parsedAge)
                || !TryEnum(profileRow, "activity_level", out ActivityLevel parsedActivity))
            {
                return Failure("profileTransfer.field.required", "Errors.ProfileTransferFileInvalid");
            }

            heightCm = parsedHeight;
            ageYears = parsedAge;
            activityLevel = parsedActivity;
        }

        ProfileSettings? settings = null;
        if (heightCm.HasValue && ageYears.HasValue && activityLevel.HasValue)
        {
            var settingsResult = ProfileSettings.Create(heightCm.Value, ageYears.Value, activityLevel.Value);
            if (!settingsResult.IsSuccess)
            {
                return Failure("profileTransfer.profile.invalid", "Errors.ProfileTransferFileInvalid");
            }

            settings = settingsResult.Value;
        }

        var profileResult = Profile.Rehydrate(
            new ProfileId(sourceProfileId),
            name,
            settings,
            createdAtUtc,
            updatedAtUtc,
            gender);
        if (!profileResult.IsSuccess)
        {
            return Failure("profileTransfer.profile.invalid", "Errors.ProfileTransferFileInvalid");
        }

        var measurements = new List<ProfileTransferMeasurement>();
        var measurementIds = new HashSet<Guid>();
        foreach (var row in RowsOfType(rows, "measurement"))
        {
            if (!TryGuid(row, "source_profile_id", out var rowProfileId)
                || rowProfileId != sourceProfileId
                || !TryGuid(row, "source_measurement_id", out var measurementId)
                || !TryEnum(row, "measurement_type", out MeasurementType type)
                || !TryUtcTimestamp(row, "measured_at_utc", out var measuredAtUtc)
                || !TryDecimal(row, "weight_kg", out var weightKg)
                || !TryDecimal(row, "height_cm", out var measurementHeightCm)
                || !TryNullableDecimal(row, "neck_cm", out var neckCm)
                || !TryNullableDecimal(row, "abdomen_cm", out var abdomenCm)
                || !TryNullableDecimal(row, "hip_cm", out var hipCm)
                || !TryEnum(row, "measurement_gender", out ProfileGender measurementGender)
                || !TryInt(row, "measurement_age_years", out var measurementAgeYears)
                || !TryEnum(row, "measurement_activity_level", out ActivityLevel measurementActivityLevel))
            {
                return Failure("profileTransfer.field.invalid", "Errors.ProfileTransferFileInvalid");
            }

            if (!measurementIds.Add(measurementId))
            {
                return Failure("profileTransfer.measurementId.duplicate", "Errors.ProfileTransferFileInvalid");
            }

            var measurementInput = new MeasurementInput(
                type,
                weightKg,
                measurementHeightCm,
                neckCm,
                abdomenCm,
                measurementAgeYears,
                measurementActivityLevel,
                measuredAtUtc,
                hipCm,
                measurementGender);
            var measurementResult = Measurement.Rehydrate(new MeasurementId(measurementId), new ProfileId(sourceProfileId), measurementInput);
            if (!measurementResult.IsSuccess)
            {
                return Failure("profileTransfer.measurement.invalid", "Errors.ProfileTransferFileInvalid");
            }

            measurements.Add(new ProfileTransferMeasurement(
                sourceProfileId,
                measurementId,
                type,
                measuredAtUtc,
                weightKg,
                measurementHeightCm,
                neckCm,
                abdomenCm,
                hipCm,
                measurementGender,
                measurementAgeYears,
                measurementActivityLevel));
        }

        var results = new List<ProfileTransferCalculationResult>();
        var resultIds = new HashSet<Guid>();
        var validMeasurementIds = measurements.Select(measurement => measurement.SourceMeasurementId).ToHashSet();
        foreach (var row in RowsOfType(rows, "calculation_result"))
        {
            if (!TryGuid(row, "source_measurement_id", out var resultMeasurementId)
                || !validMeasurementIds.Contains(resultMeasurementId)
                || !TryGuid(row, "source_result_id", out var resultId)
                || !TryEnum(row, "calculation_type", out CalculationType calculationType)
                || !TryDecimal(row, "value", out var value)
                || !TryRequired(row, "unit", out var unit)
                || !TryRequired(row, "formula_id", out var formulaId)
                || !TryRequired(row, "formula_version", out var formulaVersion)
                || !TryUtcTimestamp(row, "calculated_at_utc", out var calculatedAtUtc))
            {
                return Failure(
                    !validMeasurementIds.Contains(resultMeasurementId)
                        ? "profileTransfer.measurementReference.invalid"
                        : "profileTransfer.field.required",
                    "Errors.ProfileTransferFileInvalid");
            }

            if (!resultIds.Add(resultId))
            {
                return Failure("profileTransfer.resultId.duplicate", "Errors.ProfileTransferFileInvalid");
            }

            var result = CalculationResult.Rehydrate(
                new CalculationResultId(resultId),
                new MeasurementId(resultMeasurementId),
                calculationType,
                new CalculationResultValue(value, unit, formulaId, formulaVersion),
                calculatedAtUtc);
            if (!result.IsSuccess)
            {
                return Failure("profileTransfer.result.invalid", "Errors.ProfileTransferFileInvalid");
            }

            results.Add(new ProfileTransferCalculationResult(
                resultId,
                resultMeasurementId,
                calculationType,
                value,
                unit,
                formulaId,
                formulaVersion,
                calculatedAtUtc));
        }

        return Result.Success(new ProfileTransferDocument(
            version,
            exportedAtUtc,
            new ProfileTransferProfile(sourceProfileId, name, gender, heightCm, ageYears, activityLevel, createdAtUtc, updatedAtUtc),
            measurements,
            results));
    }

    private static List<IReadOnlyList<string>> ParseRows(string text)
    {
        var rows = new List<IReadOnlyList<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (inQuotes)
            {
                if (character == '"' && index + 1 < text.Length && text[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else if (character == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    field.Append(character);
                }

                continue;
            }

            if (character == '"' && field.Length == 0)
            {
                inQuotes = true;
            }
            else if (character == ',')
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if (character == '\n')
            {
                row.Add(field.ToString().TrimEnd('\r'));
                field.Clear();
                if (row.Any(value => value.Length > 0))
                {
                    rows.Add(row.ToArray());
                }

                row = new List<string>();
            }
            else
            {
                field.Append(character);
            }
        }

        if (inQuotes)
        {
            throw new FormatException();
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString().TrimEnd('\r'));
            rows.Add(row.ToArray());
        }

        return rows;
    }

    private static List<IReadOnlyList<string>> RowsOfType(
        IReadOnlyList<IReadOnlyList<string>> rows,
        string type)
        => rows.Where(row => row.Count == Columns.Length && row[0] == type).ToList();

    private static string?[] Row(
        string recordType,
        int? formatVersion = null,
        string? exportedAtUtc = null,
        string? sourceProfileId = null,
        string? sourceMeasurementId = null,
        string? sourceResultId = null,
        string? name = null,
        string? gender = null,
        decimal? heightCm = null,
        int? ageYears = null,
        string? activityLevel = null,
        string? createdAtUtc = null,
        string? updatedAtUtc = null,
        string? measuredAtUtc = null,
        string? measurementType = null,
        decimal? weightKg = null,
        decimal? neckCm = null,
        decimal? abdomenCm = null,
        decimal? hipCm = null,
        string? measurementGender = null,
        int? measurementAgeYears = null,
        string? measurementActivityLevel = null,
        string? calculationType = null,
        decimal? value = null,
        string? unit = null,
        string? formulaId = null,
        string? formulaVersion = null,
        string? calculatedAtUtc = null)
        =>
        [
            recordType,
            formatVersion?.ToString(CultureInfo.InvariantCulture),
            exportedAtUtc,
            sourceProfileId,
            sourceMeasurementId,
            sourceResultId,
            name,
            gender,
            heightCm?.ToString(CultureInfo.InvariantCulture),
            ageYears?.ToString(CultureInfo.InvariantCulture),
            activityLevel,
            createdAtUtc,
            updatedAtUtc,
            measuredAtUtc,
            measurementType,
            weightKg?.ToString(CultureInfo.InvariantCulture),
            neckCm?.ToString(CultureInfo.InvariantCulture),
            abdomenCm?.ToString(CultureInfo.InvariantCulture),
            hipCm?.ToString(CultureInfo.InvariantCulture),
            measurementGender,
            measurementAgeYears?.ToString(CultureInfo.InvariantCulture),
            measurementActivityLevel,
            calculationType,
            value?.ToString(CultureInfo.InvariantCulture),
            unit,
            formulaId,
            formulaVersion,
            calculatedAtUtc
        ]!;

    private static void AppendRow(StringBuilder builder, IEnumerable<string?> values)
    {
        builder.AppendJoin(',', values.Select(Escape));
        builder.AppendLine();
    }

    private static string Escape(string? value)
    {
        value ??= string.Empty;
        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
    }

    private static string FormatTimestamp(DateTimeOffset value)
        => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static int ColumnIndex(string name)
        => Array.IndexOf(Columns, name);

    private static bool TryRequired(IReadOnlyList<string> row, string column, out string value)
    {
        value = row[ColumnIndex(column)];
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryGuid(IReadOnlyList<string> row, string column, out Guid value)
        => Guid.TryParse(row[ColumnIndex(column)], out value) && value != Guid.Empty;

    private static bool TryDecimal(IReadOnlyList<string> row, string column, out decimal value)
        => decimal.TryParse(row[ColumnIndex(column)], NumberStyles.Number, CultureInfo.InvariantCulture, out value);

    private static bool TryNullableDecimal(IReadOnlyList<string> row, string column, out decimal? value)
    {
        var text = row[ColumnIndex(column)];
        if (string.IsNullOrWhiteSpace(text))
        {
            value = null;
            return true;
        }

        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            value = parsed;
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryInt(IReadOnlyList<string> row, string column, out int value)
        => int.TryParse(row[ColumnIndex(column)], NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

    private static bool TryEnum<T>(IReadOnlyList<string> row, string column, out T value)
        where T : struct, Enum
        => Enum.TryParse(row[ColumnIndex(column)], ignoreCase: true, out value) && Enum.IsDefined(value);

    private static bool TryUtcTimestamp(IReadOnlyList<string> row, string column, out DateTimeOffset value)
        => DateTimeOffset.TryParse(
            row[ColumnIndex(column)],
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out value)
            && value.Offset == TimeSpan.Zero;

    private static Result<ProfileTransferDocument> Failure(string code, string messageKey)
        => Result.Failure<ProfileTransferDocument>(new DomainError(code, messageKey));
}
