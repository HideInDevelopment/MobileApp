using SQLite;

namespace Anthropometry.Infrastructure.Persistence.Migrations;

public sealed class Migration0003 : IMigration
{
    public int Version => 3;

    public void Apply(SQLiteConnection connection)
        => connection.Execute("ALTER TABLE Profiles ADD COLUMN Gender INTEGER NOT NULL DEFAULT 1");
}
