using Sentinel.Api.Services;

namespace Sentinel.Api.Models;

public class QueryContext
{
    public required string UserPrompt { get; init; }
    public string? GeneratedSql { get; set; }
    public List<GateResult> GateResults { get; } = [];
    public SqlExecutionResult? ExecutionResult { get; set; }
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public double ElapsedMs { get; set; }
}
