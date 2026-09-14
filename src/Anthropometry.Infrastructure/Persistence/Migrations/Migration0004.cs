using SQLite;

namespace Anthropometry.Infrastructure.Persistence.Migrations;

public sealed class Migration0004 : IMigration
{
    public int Version => 4;

    public void Apply(SQLiteConnection connection)
    {
        connection.Execute("ALTER TABLE Measurements ADD COLUMN HipCm NUMERIC NULL");
        connection.Execute("ALTER TABLE Measurements ADD COLUMN Gender INTEGER NOT NULL DEFAULT 1");
    }
}
