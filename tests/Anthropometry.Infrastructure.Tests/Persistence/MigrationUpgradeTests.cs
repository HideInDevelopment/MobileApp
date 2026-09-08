using Anthropometry.Infrastructure.Persistence.Migrations;
using Anthropometry.Infrastructure.Persistence.Sqlite;
using Anthropometry.Infrastructure.Repositories;
using Anthropometry.Infrastructure.Tests.Support;
using SQLite;

namespace Anthropometry.Infrastructure.Tests.Persistence;

public sealed class MigrationUpgradeTests
{
    [Fact]
    public async Task Upgrade_from_schema_one_keeps_existing_profiles_readable()
    {
        using var database = new TemporaryDatabase();
        var factory = new SqliteConnectionFactory(database.Path);
        await new MigrationRunner(factory).InitializeAsync(CancellationToken.None);
        var profiles = new SqliteProfileRepository(factory);
        var profile = TestData.Profile();
        await profiles.AddAsync(profile, CancellationToken.None);

        await new MigrationRunner(factory, [new Migration0001(), new Migration0002Fixture()]).InitializeAsync(CancellationToken.None);

        var loaded = await profiles.GetByIdAsync(profile.Id, CancellationToken.None);
        using var connection = factory.Create();
        var version = connection.ExecuteScalar<string>("SELECT Value FROM SchemaMetadata WHERE Key = 'schema.version'");

        Assert.Equal(profile.Id, loaded!.Id);
        Assert.Equal(profile.Name, loaded.Name);
        Assert.Equal("2", version);
    }

    private sealed class Migration0002Fixture : IMigration
    {
        public int Version => 2;

        public void Apply(SQLiteConnection connection)
            => connection.Execute("ALTER TABLE Profiles ADD COLUMN Archived INTEGER NOT NULL DEFAULT 0");
    }
}
