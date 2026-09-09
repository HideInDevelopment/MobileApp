using SQLite;

namespace Anthropometry.Infrastructure.Persistence.Migrations;

public sealed class Migration0002 : IMigration
{
    public int Version => 2;

    public void Apply(SQLiteConnection connection)
    {
        connection.Execute("ALTER TABLE Profiles ADD COLUMN HeightCm NUMERIC NULL");
        connection.Execute("ALTER TABLE Profiles ADD COLUMN AgeYears INTEGER NULL");
        connection.Execute("ALTER TABLE Profiles ADD COLUMN ActivityLevel INTEGER NULL");

        connection.Execute("DROP INDEX IF EXISTS IX_Measurements_ProfileId");
        connection.Execute("DROP INDEX IF EXISTS IX_Measurements_ProfileId_MeasuredAtUtc");
        connection.Execute("DROP INDEX IF EXISTS IX_CalculationResults_MeasurementId");
        connection.Execute("DROP INDEX IF EXISTS IX_CalculationResults_CalculatedAtUtc");

        connection.Execute("ALTER TABLE CalculationResults RENAME TO CalculationResults_v1");
        connection.Execute("ALTER TABLE Measurements RENAME TO Measurements_v1");

        connection.Execute("""
            CREATE TABLE Measurements (
                Id TEXT NOT NULL PRIMARY KEY,
                ProfileId TEXT NOT NULL,
                MeasuredAtUtc TEXT NOT NULL,
                MeasurementType INTEGER NOT NULL,
                WeightKg NUMERIC NOT NULL,
                HeightCm NUMERIC NOT NULL,
                NeckCm NUMERIC NULL,
                AbdomenCm NUMERIC NULL,
                AgeYears INTEGER NOT NULL,
                ActivityLevel INTEGER NOT NULL,
                FOREIGN KEY (ProfileId) REFERENCES Profiles (Id) ON DELETE CASCADE
            )
            """);

        connection.Execute("""
            INSERT INTO Measurements (
                Id, ProfileId, MeasuredAtUtc, MeasurementType, WeightKg,
                HeightCm, NeckCm, AbdomenCm, AgeYears, ActivityLevel)
            SELECT Id, ProfileId, MeasuredAtUtc, 2, WeightKg,
                HeightCm, NeckCm, AbdomenCm, AgeYears, ActivityLevel
            FROM Measurements_v1
            """);

        connection.Execute("""
            CREATE TABLE CalculationResults (
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

        connection.Execute("""
            INSERT INTO CalculationResults (
                Id, MeasurementId, CalculationType, FormulaId, FormulaVersion,
                Value, Unit, CalculatedAtUtc)
            SELECT Id, MeasurementId, CalculationType, FormulaId, FormulaVersion,
                Value, Unit, CalculatedAtUtc
            FROM CalculationResults_v1
            """);

        connection.Execute("DROP TABLE CalculationResults_v1");
        connection.Execute("DROP TABLE Measurements_v1");

        connection.Execute("""
            UPDATE Profiles
            SET HeightCm = (SELECT HeightCm FROM Measurements WHERE Measurements.ProfileId = Profiles.Id ORDER BY MeasuredAtUtc DESC LIMIT 1),
                AgeYears = (SELECT AgeYears FROM Measurements WHERE Measurements.ProfileId = Profiles.Id ORDER BY MeasuredAtUtc DESC LIMIT 1),
                ActivityLevel = (SELECT ActivityLevel FROM Measurements WHERE Measurements.ProfileId = Profiles.Id ORDER BY MeasuredAtUtc DESC LIMIT 1)
            WHERE EXISTS (SELECT 1 FROM Measurements WHERE Measurements.ProfileId = Profiles.Id)
            """);

        connection.Execute("CREATE INDEX IF NOT EXISTS IX_Measurements_ProfileId ON Measurements (ProfileId)");
        connection.Execute("CREATE INDEX IF NOT EXISTS IX_Measurements_ProfileId_MeasuredAtUtc ON Measurements (ProfileId, MeasuredAtUtc DESC)");
        connection.Execute("CREATE INDEX IF NOT EXISTS IX_CalculationResults_MeasurementId ON CalculationResults (MeasurementId)");
        connection.Execute("CREATE INDEX IF NOT EXISTS IX_CalculationResults_CalculatedAtUtc ON CalculationResults (CalculatedAtUtc DESC)");
    }
}
