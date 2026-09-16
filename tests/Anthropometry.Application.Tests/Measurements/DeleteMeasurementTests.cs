using Anthropometry.Application.Measurements;
using Anthropometry.Application.Tests.Support;

namespace Anthropometry.Application.Tests.Measurements;

public sealed class DeleteMeasurementTests
{
    [Fact]
    public async Task Delete_removes_measurement_and_its_results()
    {
        var profile = TestData.Profile();
        var measurements = new FakeMeasurementRepository();
        var measurement = TestData.Measurement(profile.Id);
        measurements.Items.Add(measurement);

        var result = await new DeleteMeasurement(measurements).ExecuteAsync(profile.Id, measurement.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(measurements.Items);
    }

    [Fact]
    public async Task Delete_returns_not_found_without_removing_another_profiles_measurement()
    {
        var owner = TestData.Profile();
        var otherProfile = TestData.Profile("Other");
        var measurements = new FakeMeasurementRepository();
        var measurement = TestData.Measurement(owner.Id);
        measurements.Items.Add(measurement);

        var result = await new DeleteMeasurement(measurements).ExecuteAsync(otherProfile.Id, measurement.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("measurement.notFound", result.Error!.Code);
        Assert.Single(measurements.Items);
    }
}
