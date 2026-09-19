using System.Text;
using Anthropometry.Application.Profiles;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Tests.Profiles;

public sealed class ProfileTransferCsvSerializerTests
{
    [Fact]
    public void Serialize_and_parse_preserves_the_complete_profile_graph()
    {
        var snapshot = CreateSnapshot();

        var parsed = ProfileTransferCsvSerializer.Parse(
            new MemoryStream(ProfileTransferCsvSerializer.Serialize(snapshot)));

        Assert.True(parsed.IsSuccess);
        Assert.Equal(1, parsed.Value.FormatVersion);
        Assert.Equal(snapshot.Profile.Id.Value, parsed.Value.Profile.SourceProfileId);
        Assert.Equal(snapshot.Profile.Name, parsed.Value.Profile.Name);
        Assert.Equal(ProfileGender.Female, parsed.Value.Profile.Gender);
        Assert.Equal(178.25m, parsed.Value.Profile.HeightCm);
        Assert.Equal(2, parsed.Value.Measurements.Count);
        Assert.Equal(42.75m, parsed.Value.Measurements[0].HipCm);
        Assert.Equal(MeasurementType.WeightOnly, parsed.Value.Measurements[1].Type);
        Assert.Equal(3, parsed.Value.Results.Count);
        Assert.Equal("female.body-fat.us-navy", parsed.Value.Results[0].FormulaId);
        Assert.Equal("2.0", parsed.Value.Results[0].FormulaVersion);
        Assert.Equal("percent", parsed.Value.Results[0].Unit);
        Assert.Equal(23.45m, parsed.Value.Results[0].Value);
    }

    [Fact]
    public void Serialize_uses_invariant_decimal_and_csv_escaping()
    {
        var snapshot = CreateSnapshot("Anna, \"A\"\nTest");
        var text = Encoding.UTF8.GetString(ProfileTransferCsvSerializer.Serialize(snapshot));

        Assert.Contains("178.25", text);
        Assert.DoesNotContain("178,25", text);
        Assert.Contains("\"Anna, \"\"A\"\"" + "\n" + "Test\"", text);
    }

    [Fact]
    public void Parse_accepts_utf8_bom()
    {
        var content = ProfileTransferCsvSerializer.Serialize(CreateSnapshot());
        var withBom = new byte[Encoding.UTF8.GetPreamble().Length + content.Length];
        Encoding.UTF8.GetPreamble().CopyTo(withBom, 0);
        content.CopyTo(withBom, Encoding.UTF8.GetPreamble().Length);

        var parsed = ProfileTransferCsvSerializer.Parse(new MemoryStream(withBom));

        Assert.True(parsed.IsSuccess);
    }

    [Fact]
    public void Parse_rejects_duplicate_source_ids()
    {
        var snapshot = CreateSnapshot();
        var content = Encoding.UTF8.GetString(ProfileTransferCsvSerializer.Serialize(snapshot));
        content = content.Replace(
            snapshot.Measurements[1].Id.ToString(),
            snapshot.Measurements[0].Id.ToString(),
            StringComparison.Ordinal);

        var parsed = ProfileTransferCsvSerializer.Parse(new MemoryStream(Encoding.UTF8.GetBytes(content)));

        Assert.False(parsed.IsSuccess);
        Assert.Equal("profileTransfer.measurementId.duplicate", parsed.Error!.Code);
    }

    [Fact]
    public void Parse_rejects_result_referencing_an_unknown_measurement()
    {
        var snapshot = CreateSnapshot();
        var content = Encoding.UTF8.GetString(ProfileTransferCsvSerializer.Serialize(snapshot));
        content = ReplaceLast(content, snapshot.Measurements[0].Id.ToString(), Guid.NewGuid().ToString("D"));

        var parsed = ProfileTransferCsvSerializer.Parse(new MemoryStream(Encoding.UTF8.GetBytes(content)));

        Assert.False(parsed.IsSuccess);
        Assert.Equal("profileTransfer.measurementReference.invalid", parsed.Error!.Code);
    }

    [Fact]
    public void Parse_rejects_unsupported_format_version()
    {
        var content = Encoding.UTF8.GetString(ProfileTransferCsvSerializer.Serialize(CreateSnapshot()))
            .Replace("meta,1,", "meta,2,", StringComparison.Ordinal);

        var parsed = ProfileTransferCsvSerializer.Parse(new MemoryStream(Encoding.UTF8.GetBytes(content)));

        Assert.False(parsed.IsSuccess);
        Assert.Equal("profileTransfer.formatVersion.unsupported", parsed.Error!.Code);
    }

    [Fact]
    public void Parse_rejects_malformed_required_fields()
    {
        var content = Encoding.UTF8.GetString(ProfileTransferCsvSerializer.Serialize(CreateSnapshot()))
            .Replace("female.body-fat.us-navy", "", StringComparison.Ordinal);

        var parsed = ProfileTransferCsvSerializer.Parse(new MemoryStream(Encoding.UTF8.GetBytes(content)));

        Assert.False(parsed.IsSuccess);
        Assert.Equal("profileTransfer.field.required", parsed.Error!.Code);
    }

    private static ProfileTransferSnapshot CreateSnapshot(string? name = null)
    {
        var profileId = ProfileId.New();
        var createdAt = new DateTimeOffset(2026, 9, 1, 8, 30, 0, TimeSpan.Zero);
        var profile = Profile.Rehydrate(
            profileId,
            name ?? "Anna",
            ProfileSettings.Create(178.25m, 31, ActivityLevel.High).Value,
            createdAt,
            createdAt.AddDays(1),
            ProfileGender.Female).Value;

        var sized = Measurement.Rehydrate(
            MeasurementId.New(),
            profileId,
            new MeasurementInput(
                MeasurementType.WeightAndSizes,
                72.5m,
                178.25m,
                31.5m,
                84.75m,
                31,
                ActivityLevel.High,
                createdAt.AddDays(2),
                42.75m,
                ProfileGender.Female)).Value;
        var weightOnly = Measurement.Rehydrate(
            MeasurementId.New(),
            profileId,
            new MeasurementInput(
                MeasurementType.WeightOnly,
                71.25m,
                178.25m,
                null,
                null,
                31,
                ActivityLevel.High,
                createdAt.AddDays(3),
                null,
                ProfileGender.Female)).Value;

        var results = new[]
        {
            CalculationResult.Rehydrate(
                CalculationResultId.New(),
                sized.Id,
                CalculationType.BodyFatPercentage,
                new CalculationResultValue(23.45m, "percent", "female.body-fat.us-navy", "2.0"),
                createdAt.AddDays(2).AddMinutes(1)).Value,
            CalculationResult.Rehydrate(
                CalculationResultId.New(),
                sized.Id,
                CalculationType.BasalMetabolicRate,
                new CalculationResultValue(1488.25m, "kcal/day", "female.bmr.mifflin-st-jeor", "2.0"),
                createdAt.AddDays(2).AddMinutes(1)).Value,
            CalculationResult.Rehydrate(
                CalculationResultId.New(),
                weightOnly.Id,
                CalculationType.TotalDailyEnergyExpenditure,
                new CalculationResultValue(2300.75m, "kcal/day", "tdee.activity-multiplier", "1.0"),
                createdAt.AddDays(3).AddMinutes(1)).Value
        };

        return new ProfileTransferSnapshot(profile, [sized, weightOnly], results);
    }

    private static string ReplaceLast(string text, string oldValue, string newValue)
    {
        var index = text.LastIndexOf(oldValue, StringComparison.Ordinal);
        Assert.True(index >= 0);
        return text[..index] + newValue + text[(index + oldValue.Length)..];
    }
}
