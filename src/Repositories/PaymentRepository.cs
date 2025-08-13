using Dapper;

namespace Rinha.Api.Repositories;

public sealed class PaymentRepository(NpgsqlDataSource dbSource)
{
    public async Task AddAsync(Payment payment, PaymentGateway gateway)
    {
        using var connection = await dbSource.OpenConnectionAsync();

        var parameters = new DynamicParameters();
        parameters.Add("CorrelationId", payment.CorrelationId);
        parameters.Add("Amount", payment.Amount);
        parameters.Add("Requested_At", payment.RequestedAt);
        parameters.Add("Gateway", gateway.ToString());

        var sql = @"INSERT INTO payments (correlationId, amount, requested_at, gateway)
                  VALUES(@CorrelationId, @Amount, @Requested_At, @Gateway);";

        await connection.ExecuteAsync(sql, parameters);
    }

    public async Task<SummaryResponse> GetSummaryAsync(DateTimeOffset? fromUtc, DateTimeOffset? toUtc)
    {
        using var connection = await dbSource.OpenConnectionAsync();

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
}