using System.Data;

using Dapper;

namespace Rinha.Api.Repositories;

public class PaymentRepository(DatabaseConnection connection)
{
    public async Task AddAsync(Payment payment, PaymentGateway gateway)
    {
        var sql = @"INSERT INTO payments (correlationId, amount, requested_at, gateway)
                  VALUES(@CorrelationId, @Amount, @Requested_At, @Gateway);";

        var _context = connection.GetConnection();
        await _context.ExecuteAsync(sql, new
        {
            payment.CorrelationId,
            payment.Amount,
            Requested_At = payment.RequestedAt,
            Gateway = gateway
        });

        _context.Dispose();
    }

    public async Task<SummaryResponse> GetSummaryAsync(DateTime? fromUtc, DateTime? toUtc)
    {
        IDbConnection _context = connection.GetConnection();
        try
        {

            var parameters = new DynamicParameters();

            parameters.Add(
                name: "fromUtc",
                dbType: DbType.DateTimeOffset,
                value: fromUtc.HasValue ? DateTime.SpecifyKind(fromUtc.Value, DateTimeKind.Utc) : DBNull.Value);

            parameters.Add(
                name: "toUtc",
                dbType: DbType.DateTimeOffset,
                value: toUtc.HasValue ? DateTime.SpecifyKind(toUtc.Value, DateTimeKind.Utc) : DBNull.Value);

            const string sql = @"
                SELECT gateway,
                    COUNT(*) AS TotalRequests,
                    SUM(amount) AS TotalAmount
                FROM payments
                WHERE requested_at BETWEEN @fromUtc AND @toUtc
                    OR @fromUtc is NULL
                    OR @toUtc is NULL
                GROUP BY gateway;";

            var result = await _context.QueryAsync<Summary>(sql, parameters);

            var @default = result.FirstOrDefault(x => x.gateway == PaymentGateway.Default)
                ?? new Summary(PaymentGateway.Default, 0, 0);

            var fallback = result.FirstOrDefault(x => x.gateway == PaymentGateway.Fallback)
                ?? new Summary(PaymentGateway.Fallback, 0, 0);

            return new SummaryResponse(
                new SummaryItem(@default.TotalRequests, @default.TotalAmount),
                new SummaryItem(fallback.TotalRequests, fallback.TotalAmount));
        }
        finally
        {
            _context.Dispose();
        }
    }
}