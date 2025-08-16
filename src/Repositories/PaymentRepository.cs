using Rinha.Api.Models.Payments;
using Rinha.Api.Models.Summaries;

namespace Rinha.Api.Repositories;

public sealed class PaymentRepository(NpgsqlDataSource dbSource)
{
    public async Task AddRangeAsync(IEnumerable<Payment> payments)
    {
        using var connection = await dbSource.OpenConnectionAsync();

        var sql = @"INSERT INTO payments (correlationId, amount, requested_at, gateway)
                  VALUES(@CorrelationId, @Amount, @Requested_At, @Gateway);";

        try
        {

            await connection.ExecuteAsync(sql, payments.Select(r =>
            {
                return new
                {
                    r.CorrelationId,
                    r.Amount,
                    Requested_At = r.RequestedAt,
                    Gateway = r.Gateway.ToString(),
                };
            }));
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public async Task<SummaryResponse> GetSummaryAsync(DateTimeOffset? fromUtc, DateTimeOffset? toUtc)
    {

        using var connection = await dbSource.OpenConnectionAsync();

        try
        {

            const string sql = @"
                SELECT gateway,
                    COUNT(*) AS TotalRequests,
                    SUM(amount) AS TotalAmount
                FROM payments
                WHERE requested_at BETWEEN @fromUtc AND @toUtc
                    OR @fromUtc is NULL
                    OR @toUtc is NULL
                GROUP BY gateway;";

            var result = await connection.QueryAsync<Summary>(sql, new
            {
                fromUtc,
                toUtc
            });

            var @default = result.FirstOrDefault(x => x.gateway == PaymentGateway.Default.ToString())
                ?? new Summary(PaymentGateway.Default.ToString(), 0, 0);

            var fallback = result.FirstOrDefault(x => x.gateway == PaymentGateway.Fallback.ToString())
                ?? new Summary(PaymentGateway.Fallback.ToString(), 0, 0);

            return new SummaryResponse(
                new SummaryItem(@default.TotalRequests, @default.TotalAmount),
                new SummaryItem(fallback.TotalRequests, fallback.TotalAmount));
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}