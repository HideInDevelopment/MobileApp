using SQLite;

namespace Anthropometry.Infrastructure.Persistence.Migrations;

public sealed class Migration0001 : IMigration
{
    public int Version => 1;

    public void Apply(SQLiteConnection connection)
    {
        connection.Execute("""
            CREATE TABLE IF NOT EXISTS Profiles (
                Id TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                UpdatedAtUtc TEXT NOT NULL
            )
            """);

        connection.Execute("""
            CREATE TABLE IF NOT EXISTS Measurements (
                Id TEXT NOT NULL PRIMARY KEY,
                ProfileId TEXT NOT NULL,
                MeasuredAtUtc TEXT NOT NULL,
                WeightKg NUMERIC NOT NULL,
                HeightCm NUMERIC NOT NULL,
                NeckCm NUMERIC NOT NULL,
                AbdomenCm NUMERIC NOT NULL,
                AgeYears INTEGER NOT NULL,
                ActivityLevel INTEGER NOT NULL,
                FOREIGN KEY (ProfileId) REFERENCES Profiles (Id) ON DELETE CASCADE
            )
            """);

        connection.Execute("""
            CREATE TABLE IF NOT EXISTS CalculationResults (
                Id TEXT NOT NULL PRIMARY KEY,
                MeasurementId TEXT NOT NULL,
                CalculationType INTEGER NOT NULL,
                FormulaId TEXT NOT NULL,
                FormulaVersion TEXT NOT NULL,
                Value NUMERIC NOT NULL,
                Unit TEXT NOT NULL,
                CalculatedAtUtc TEXT NOT NULL,
                FOREIGN KEY (MeasurementId) REFERENCES Measurements (Id) ON DELETE CASCADE
            )
            """);

        connection.Execute("CREATE INDEX IF NOT EXISTS IX_Measurements_ProfileId ON Measurements (ProfileId)");
        connection.Execute("CREATE INDEX IF NOT EXISTS IX_Measurements_ProfileId_MeasuredAtUtc ON Measurements (ProfileId, MeasuredAtUtc DESC)");
        connection.Execute("CREATE INDEX IF NOT EXISTS IX_CalculationResults_MeasurementId ON CalculationResults (MeasurementId)");
        connection.Execute("CREATE INDEX IF NOT EXISTS IX_CalculationResults_CalculatedAtUtc ON CalculationResults (CalculatedAtUtc DESC)");
    }
}
