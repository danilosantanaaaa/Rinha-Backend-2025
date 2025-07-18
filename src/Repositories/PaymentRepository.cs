using System.Data;

using Dapper;

using Rinha.Api.Helpers;
using Rinha.Api.Models;

namespace Rinha.Api.Repositories;

public class PaymentRepository(DatabaseConnection connection)
{
    private readonly IDbConnection _context = connection.GetConnection();

    public async Task AddAsync(Payment model, PaymentProcessorType type)
    {
        var sql = @"INSERT INTO payments (correlationId, amount, requested_at, type)
                  VALUES(@CorrelationId, @Amount, @Requested_At, @Type);";

        await _context.ExecuteAsync(sql, new
        {
            model.CorrelationId,
            model.Amount,
            Requested_At = model.RequestedAt,
            Type = type
        });
    }

    public async Task<SummaryResponse> GetSummaryAsync(DateTime from, DateTime to)
    {
        const string sql = @"
                SELECT type,
                    COUNT(*) AS TotalRequests,
                    SUM(amount) AS TotalAmount
                FROM payments
                WHERE (@from IS NULL OR requested_at >= @from)
                AND (@to IS NULL OR requested_at <= @to)
                GROUP BY type;";

        var result = await _context.QueryAsync<Summary>(sql, new
        {
            from,
            to
        });

        var @default = result.FirstOrDefault(x => x.Type == PaymentProcessorType.Default)
            ?? new Summary(PaymentProcessorType.Default, 0, 0);

        var fallback = result.FirstOrDefault(x => x.Type == PaymentProcessorType.Fallback)
            ?? new Summary(PaymentProcessorType.Fallback, 0, 0);

        return new SummaryResponse(
            new SummaryItem(@default.TotalRequests, @default.TotalAmount),
            new SummaryItem(fallback.TotalRequests, fallback.TotalAmount));
    }
}