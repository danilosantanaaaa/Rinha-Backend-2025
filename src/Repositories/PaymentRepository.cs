using Dapper;

using Rinha.Api.Helpers;
using Rinha.Api.Models;

namespace Rinha.Api.Repositories;

public class PaymentRepository(DatabaseConnection connection)
{
    public async Task AddAsync(Payment model, PaymentGateway type)
    {
        var sql = @"INSERT INTO payments (correlationId, amount, requested_at, type)
                  VALUES(@CorrelationId, @Amount, @Requested_At, @Type);";

        var _context = connection.GetConnection();
        await _context.ExecuteAsync(sql, new
        {
            model.CorrelationId,
            model.Amount,
            Requested_At = model.RequestedAt,
            Type = type
        });

        _context.Dispose();
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

        var _context = connection.GetConnection();

        // TODO: retirar essa gambiara e usar no banco o DateTime UTC para não precisar converter
        from = from.ToLocalTime();
        to = to.ToLocalTime();

        var result = await _context.QueryAsync<Summary>(sql, new
        {
            from,
            to
        });

        _context.Dispose();

        var @default = result.FirstOrDefault(x => x.Type == PaymentGateway.Default)
            ?? new Summary(PaymentGateway.Default, 0, 0);

        var fallback = result.FirstOrDefault(x => x.Type == PaymentGateway.Fallback)
            ?? new Summary(PaymentGateway.Fallback, 0, 0);

        return new SummaryResponse(
            new SummaryItem(@default.TotalRequests, @default.TotalAmount),
            new SummaryItem(fallback.TotalRequests, fallback.TotalAmount));
    }
}