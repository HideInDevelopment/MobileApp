using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Common;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Domain.Measurements;

public sealed class Measurement
{
    private Measurement(
        MeasurementId id,
        ProfileId profileId,
        MeasurementInput input)
    {
        Id = id;
        ProfileId = profileId;
        WeightKg = input.WeightKg;
        HeightCm = input.HeightCm;
        NeckCm = input.NeckCm;
        AbdomenCm = input.AbdomenCm;
        AgeYears = input.AgeYears;
        ActivityLevel = input.ActivityLevel;
        MeasuredAtUtc = input.MeasuredAtUtc;
    }

    public MeasurementId Id { get; }

    public ProfileId ProfileId { get; }

    public DateTimeOffset MeasuredAtUtc { get; }

    public decimal WeightKg { get; }

    public decimal HeightCm { get; }

    public decimal NeckCm { get; }

    public decimal AbdomenCm { get; }

    public int AgeYears { get; }

    public ActivityLevel ActivityLevel { get; }

    public static Result<Measurement> Create(ProfileId profileId, MeasurementInput input, DateTimeOffset idTime)
    {
        if (profileId.Value == Guid.Empty)
        {
            return Result.Failure<Measurement>(new DomainError("measurement.profileId.invalid", "Errors.MeasurementProfileIdInvalid"));
        }

        if (input is null)
        {
            return Result.Failure<Measurement>(new DomainError("measurement.required", "Errors.MeasurementRequired"));
        }

        var error = Validate(input);
        return error is null
            ? Result.Success(new Measurement(MeasurementId.New(), profileId, input))
            : Result.Failure<Measurement>(error);
    }

    public static Result<Measurement> Rehydrate(
        MeasurementId id,
        ProfileId profileId,
        MeasurementInput input)
    {
        if (id.Value == Guid.Empty)
        {
            return Result.Failure<Measurement>(new DomainError("measurement.id.invalid", "Errors.MeasurementIdInvalid"));
        }

        var created = Create(profileId, input, input.MeasuredAtUtc);
        return created.IsSuccess
            ? Result.Success(new Measurement(id, profileId, input))
            : created;
    }

    private static DomainError? Validate(MeasurementInput input)
    {
        if (input.WeightKg is < 1m or > 500m)
        {
            return new DomainError("measurement.weight.invalid", "Errors.MeasurementWeightInvalid");
        }

        if (input.HeightCm is < 50m or > 300m)
        {
            return new DomainError("measurement.height.invalid", "Errors.MeasurementHeightInvalid");
        }

        if (input.NeckCm is < 1m or > 100m)
        {
            return new DomainError("measurement.neck.invalid", "Errors.MeasurementNeckInvalid");
        }

        if (input.AbdomenCm is < 1m or > 400m)
        {
            return new DomainError("measurement.abdomen.invalid", "Errors.MeasurementAbdomenInvalid");
        }

        if (input.AgeYears is < 1 or > 120)
        {
            return new DomainError("measurement.age.invalid", "Errors.MeasurementAgeInvalid");
        }

        if (!Enum.IsDefined(input.ActivityLevel) || input.ActivityLevel == ActivityLevel.Unknown)
        {
            return new DomainError("measurement.activity.invalid", "Errors.MeasurementActivityInvalid");
        }

        return Guard.Utc(input.MeasuredAtUtc, "measurement.measuredAtUtc.invalid", "Errors.MeasurementMeasuredAtUtcInvalid");
    }
}
