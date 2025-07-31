using System.Data;

namespace Rinha.Api.Common;

public class DatabaseConnection
{
    private readonly string _connectionString;

    public DatabaseConnection(
        IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException("Database connection string is not configured.");
    }

    public IDbConnection GetConnection() => new NpgsqlConnection(_connectionString);


}