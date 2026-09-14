using Anthropometry.Infrastructure.Persistence.Sqlite;
using SQLite;

namespace Anthropometry.Infrastructure.Persistence.Migrations;

public sealed class MigrationRunner
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly IReadOnlyList<IMigration> _migrations;

    public MigrationRunner(SqliteConnectionFactory connectionFactory, IEnumerable<IMigration>? migrations = null)
    {
        _connectionFactory = connectionFactory;
        var configuredMigrations = migrations?.ToArray();
        if (configuredMigrations is null || configuredMigrations.Length == 0)
        {
            configuredMigrations = [new Migration0001(), new Migration0002(), new Migration0003()];
        }

        _migrations = configuredMigrations.OrderBy(migration => migration.Version).ToArray();
    }

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = _connectionFactory.Create();
            connection.Execute("CREATE TABLE IF NOT EXISTS SchemaMetadata (Key TEXT NOT NULL PRIMARY KEY, Value TEXT NOT NULL)");

            var currentVersion = ReadVersion(connection);
            foreach (var migration in _migrations.Where(migration => migration.Version > currentVersion))
            {
                cancellationToken.ThrowIfCancellationRequested();
                connection.RunInTransaction(() =>
                {
                    migration.Apply(connection);
                    connection.Execute(
                        "INSERT OR REPLACE INTO SchemaMetadata (Key, Value) VALUES (?, ?)",
                        SqliteSchema.VersionKey,
                        migration.Version.ToString(System.Globalization.CultureInfo.InvariantCulture));
                });
            }
        }, cancellationToken);
    }

    private static int ReadVersion(SQLiteConnection connection)
    {
        var value = connection.ExecuteScalar<string>(
            "SELECT Value FROM SchemaMetadata WHERE Key = ?",
            SqliteSchema.VersionKey);
        return int.TryParse(value, out var version) ? version : 0;
    }
}
