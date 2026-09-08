using SQLite;

namespace Anthropometry.Infrastructure.Persistence.Sqlite;

public sealed class SqliteConnectionFactory
{
    private readonly string _databasePath;

    public SqliteConnectionFactory(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("A database path is required.", nameof(databasePath));
        }

        _databasePath = databasePath;
    }

    public SQLiteConnection Create()
    {
        var fullPath = Path.GetFullPath(_databasePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var connection = new SQLiteConnection(
            fullPath,
            SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex,
            storeDateTimeAsTicks: false);
        connection.Execute("PRAGMA foreign_keys = ON");
        return connection;
    }
}
