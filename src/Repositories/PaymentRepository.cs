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

    public async Task<SummaryResponse> GetSummaryAsync(DateTime fromUtc, DateTime toUtc)
    {
        const string sql = @"
                SELECT gateway,
                    COUNT(*) AS TotalRequests,
                    SUM(amount) AS TotalAmount
                FROM payments
                WHERE (@from IS NULL OR requested_at >= @from)
                AND (@to IS NULL OR requested_at <= @to)
                GROUP BY gateway;";

        var _context = connection.GetConnection();

        var result = await _context.QueryAsync<Summary>(sql, new
        {
            fromUtc,
            toUtc
        });

        _context.Dispose();

        var @default = result.FirstOrDefault(x => x.gateway == PaymentGateway.Default)
            ?? new Summary(PaymentGateway.Default, 0, 0);

        var fallback = result.FirstOrDefault(x => x.gateway == PaymentGateway.Fallback)
            ?? new Summary(PaymentGateway.Fallback, 0, 0);

        return new SummaryResponse(
            new SummaryItem(@default.TotalRequests, @default.TotalAmount),
            new SummaryItem(fallback.TotalRequests, fallback.TotalAmount));
    }
}