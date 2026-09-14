using Anthropometry.Infrastructure.Persistence.Migrations;
using Anthropometry.Infrastructure.Persistence.Sqlite;
using Anthropometry.Infrastructure.Tests.Support;
using SQLite;

namespace Anthropometry.Infrastructure.Tests.Persistence;

public sealed class MigrationTests
{
    [Fact]
    public async Task Initialize_creates_schema_tables_and_records_version_four()
    {
        using var database = new TemporaryDatabase();
        var factory = new SqliteConnectionFactory(database.Path);
        var runner = new MigrationRunner(factory);

        await runner.InitializeAsync(CancellationToken.None);

        using var connection = factory.Create();
        var tables = connection.Query<TableRow>(
            "SELECT name AS Name FROM sqlite_master WHERE type = 'table' ORDER BY name");
        var version = connection.ExecuteScalar<string>(
            "SELECT Value FROM SchemaMetadata WHERE Key = 'schema.version'");

        Assert.Contains(tables, table => table.Name == "Profiles");
        Assert.Contains(tables, table => table.Name == "Measurements");
        Assert.Contains(tables, table => table.Name == "CalculationResults");
        Assert.Contains(tables, table => table.Name == "SchemaMetadata");
        Assert.Equal("4", version);

        var measurementColumns = connection.Query<ColumnRow>(
            "PRAGMA table_info(Measurements)");

        Assert.Contains(measurementColumns, column => column.Name == "HipCm");
        Assert.Contains(measurementColumns, column => column.Name == "Gender");
    }

    [Fact]
    public async Task Initialize_uses_default_migration_when_no_migrations_are_registered()
    {
        using var database = new TemporaryDatabase();
        var factory = new SqliteConnectionFactory(database.Path);
        var runner = new MigrationRunner(factory, Array.Empty<IMigration>());

        await runner.InitializeAsync(CancellationToken.None);

        using var connection = factory.Create();
        var profilesTable = connection.ExecuteScalar<string>(
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'Profiles'");

        Assert.Equal("Profiles", profilesTable);
    }

    private sealed class TableRow
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class ColumnRow
    {
        public string Name { get; set; } = string.Empty;
    }
}
