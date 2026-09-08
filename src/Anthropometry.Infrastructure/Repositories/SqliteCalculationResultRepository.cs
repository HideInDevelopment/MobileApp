using Anthropometry.Application.Abstractions;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;
using Anthropometry.Infrastructure.Persistence.Sqlite;

namespace Anthropometry.Infrastructure.Repositories;

public sealed class SqliteCalculationResultRepository : ICalculationResultRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteCalculationResultRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Task AddAsync(CalculationResult result, CancellationToken cancellationToken)
        => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = _connectionFactory.Create();
            connection.Execute(
                "INSERT INTO CalculationResults (Id, MeasurementId, CalculationType, FormulaId, FormulaVersion, Value, Unit, CalculatedAtUtc) VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                result.Id.ToString(),
                result.MeasurementId.ToString(),
                (int)result.CalculationType,
                result.FormulaId,
                result.FormulaVersion,
                result.Value,
                result.Unit,
                SqliteValueConverter.ToUtcString(result.CalculatedAtUtc));
        }, cancellationToken);

    public Task<IReadOnlyList<CalculationResult>> GetByMeasurementAsync(MeasurementId measurementId, CancellationToken cancellationToken)
        => Task.Run<IReadOnlyList<CalculationResult>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = _connectionFactory.Create();
            return connection.Query<CalculationResultRow>(
                    "SELECT Id, MeasurementId, CalculationType, FormulaId, FormulaVersion, Value, Unit, CalculatedAtUtc FROM CalculationResults WHERE MeasurementId = ? ORDER BY CalculatedAtUtc DESC",
                    measurementId.ToString())
                .Select(Map)
                .ToArray();
        }, cancellationToken);

    private static CalculationResult Map(CalculationResultRow row)
    {
        var value = new CalculationResultValue(row.Value, row.Unit, row.FormulaId, row.FormulaVersion);
        var result = CalculationResult.Rehydrate(
            new CalculationResultId(Guid.Parse(row.Id)),
            new MeasurementId(Guid.Parse(row.MeasurementId)),
            (CalculationType)row.CalculationType,
            value,
            SqliteValueConverter.ToUtcDateTimeOffset(row.CalculatedAtUtc));
        return result.IsSuccess ? result.Value : throw new InvalidDataException(result.Error!.Code);
    }

    private sealed class CalculationResultRow
    {
        public string Id { get; set; } = string.Empty;
        public string MeasurementId { get; set; } = string.Empty;
        public int CalculationType { get; set; }
        public string FormulaId { get; set; } = string.Empty;
        public string FormulaVersion { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string CalculatedAtUtc { get; set; } = string.Empty;
    }
}
