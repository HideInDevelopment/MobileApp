using Anthropometry.Application.Abstractions;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Tests.Support;

public sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
}

public sealed class FakeProfileRepository : IProfileRepository
{
    public List<Profile> Items { get; } = [];

    public int DeleteCalls { get; private set; }

    public Task<IReadOnlyList<Profile>> GetAllAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Profile>>(Items.OrderBy(profile => profile.Name).ToArray());

    public Task<Profile?> GetByIdAsync(ProfileId id, CancellationToken cancellationToken)
        => Task.FromResult(Items.SingleOrDefault(profile => profile.Id == id));

    public Task AddAsync(Profile profile, CancellationToken cancellationToken)
    {
        Items.Add(profile);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Profile profile, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task DeleteWithMeasurementsAsync(ProfileId id, CancellationToken cancellationToken)
    {
        DeleteCalls++;
        Items.RemoveAll(profile => profile.Id == id);
        return Task.CompletedTask;
    }
}

public sealed class FakeMeasurementRepository : IMeasurementRepository
{
    public List<Measurement> Items { get; } = [];

    public Task AddAsync(Measurement measurement, CancellationToken cancellationToken)
    {
        Items.Add(measurement);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Measurement measurement, CancellationToken cancellationToken)
    {
        var index = Items.FindIndex(item => item.Id == measurement.Id);
        if (index < 0)
        {
            throw new KeyNotFoundException();
        }

        Items[index] = measurement;
        return Task.CompletedTask;
    }

    public Task DeleteWithResultsAsync(MeasurementId id, CancellationToken cancellationToken)
    {
        Items.RemoveAll(measurement => measurement.Id == id);
        return Task.CompletedTask;
    }

    public Task<Measurement?> GetByIdAsync(MeasurementId id, CancellationToken cancellationToken)
        => Task.FromResult(Items.SingleOrDefault(measurement => measurement.Id == id));

    public Task<IReadOnlyList<Measurement>> GetByProfileAsync(ProfileId profileId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Measurement>>(Items.Where(measurement => measurement.ProfileId == profileId).OrderByDescending(measurement => measurement.MeasuredAtUtc).ToArray());
}

public sealed class FakeCalculationResultRepository : ICalculationResultRepository
{
    public List<CalculationResult> Items { get; } = [];

    public Task AddAsync(CalculationResult result, CancellationToken cancellationToken)
    {
        Items.Add(result);
        return Task.CompletedTask;
    }

    public Task DeleteByMeasurementAsync(MeasurementId measurementId, CancellationToken cancellationToken)
    {
        Items.RemoveAll(result => result.MeasurementId == measurementId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CalculationResult>> GetByMeasurementAsync(MeasurementId measurementId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<CalculationResult>>(Items.Where(result => result.MeasurementId == measurementId).ToArray());
}

public static class TestData
{
    public static Profile Profile(string name = "Manuel")
        => Anthropometry.Domain.Profiles.Profile.Create(
            name,
            Anthropometry.Domain.Profiles.ProfileSettings.Create(180m, 35, ActivityLevel.Moderate).Value,
            DateTimeOffset.UtcNow).Value;

    public static MeasurementInput MeasurementInput(DateTimeOffset? measuredAtUtc = null)
        => new(MeasurementType.WeightAndSizes, 80m, 180m, 40m, 90m, 35, ActivityLevel.Moderate, measuredAtUtc ?? DateTimeOffset.UtcNow);

    public static Measurement Measurement(ProfileId profileId)
        => Anthropometry.Domain.Measurements.Measurement.Create(profileId, MeasurementInput(), DateTimeOffset.UtcNow).Value;
}
