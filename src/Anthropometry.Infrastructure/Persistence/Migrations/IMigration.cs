using SQLite;

namespace Anthropometry.Infrastructure.Persistence.Migrations;

public interface IMigration
{
    int Version { get; }

    void Apply(SQLiteConnection connection);
}
