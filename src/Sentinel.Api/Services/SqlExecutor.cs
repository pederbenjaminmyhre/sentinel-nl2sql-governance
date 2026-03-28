using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentinel.Api.Configuration;

namespace Sentinel.Api.Services;

public class SqlExecutor(
    IConfiguration config,
    IOptions<SentinelOptions> options,
    ILogger<SqlExecutor> logger) : ISqlExecutor
{
    public async Task<SqlExecutionResult> ExecuteAsync(string sql)
    {
        var connectionString = config.GetConnectionString("ReadOnlyReplica")
            ?? throw new InvalidOperationException("ReadOnlyReplica connection string is not configured.");

        var opts = options.Value;
        var safeSql = InjectRowLimit(sql, opts.MaxRowCount);

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(safeSql, connection)
            {
                CommandTimeout = opts.QueryTimeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync();
            var rows = new List<Dictionary<string, object?>>();

            while (await reader.ReadAsync() && rows.Count < opts.MaxRowCount)
            {
                var row = new Dictionary<string, object?>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                rows.Add(row);
            }

            logger.LogInformation("Query executed successfully. Rows returned: {RowCount}", rows.Count);
            return SqlExecutionResult.Ok(rows);
        }
        catch (SqlException ex) when (ex.Number == -2)
        {
            logger.LogWarning("Query timed out after {Timeout}s", opts.QueryTimeoutSeconds);
            return SqlExecutionResult.Fail($"Query exceeded {opts.QueryTimeoutSeconds}-second timeout.");
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL execution error");
            return SqlExecutionResult.Fail($"SQL execution error: {ex.Message}");
        }
    }

    public static string InjectRowLimit(string sql, int maxRows)
    {
        if (sql.Contains("TOP", StringComparison.OrdinalIgnoreCase))
            return sql;

        var trimmed = sql.TrimStart();
        if (trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            return $"SELECT TOP {maxRows} " + trimmed["SELECT".Length..].TrimStart();
        }

        return sql;
    }
}
