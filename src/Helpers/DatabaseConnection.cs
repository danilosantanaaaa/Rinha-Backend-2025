using System.Data;

using Npgsql;

namespace Rinha.Api.Helpers;

public class DatabaseConnection
{
    private readonly IConfiguration _configuration;

    public DatabaseConnection(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IDbConnection GetConnection() => new NpgsqlConnection(_configuration.GetConnectionString("PostgreSQL"))
        ?? throw new InvalidOperationException("Database connection string is not configured.");
}