using Anthropometry.Infrastructure.Persistence.Migrations;
using Anthropometry.Infrastructure.Persistence.Sqlite;
using Anthropometry.Infrastructure.Repositories;
using Anthropometry.Infrastructure.Tests.Support;

namespace Anthropometry.Infrastructure.Tests.Repositories;

public sealed class SqliteCalculationResultRepositoryTests
{
    [Fact]
    public async Task Add_and_get_preserves_formula_metadata_and_value()
    {
        using var database = new TemporaryDatabase();
        var factory = new SqliteConnectionFactory(database.Path);
        await new MigrationRunner(factory).InitializeAsync(CancellationToken.None);
        var profiles = new SqliteProfileRepository(factory);
        var measurements = new SqliteMeasurementRepository(factory);
        var repository = new SqliteCalculationResultRepository(factory);
        var profile = TestData.Profile();
        var measurement = TestData.Measurement(profile.Id);
        var expected = TestData.CalculationResult(measurement.Id);

        await profiles.AddAsync(profile, CancellationToken.None);
        await measurements.AddAsync(measurement, CancellationToken.None);
        await repository.AddAsync(expected, CancellationToken.None);

        var actual = Assert.Single(await repository.GetByMeasurementAsync(measurement.Id, CancellationToken.None));

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.FormulaId, actual.FormulaId);
        Assert.Equal(expected.FormulaVersion, actual.FormulaVersion);
        Assert.Equal(expected.Value, actual.Value);
        Assert.Equal(expected.Unit, actual.Unit);
    }
}
