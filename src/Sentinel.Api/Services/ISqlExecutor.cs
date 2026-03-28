namespace Sentinel.Api.Services;

public interface ISqlExecutor
{
    Task<SqlExecutionResult> ExecuteAsync(string sql);
}

public record SqlExecutionResult(
    bool Success,
    List<Dictionary<string, object?>>? Rows,
    int RowCount,
    string? Error
)
{
    public static SqlExecutionResult Ok(List<Dictionary<string, object?>> rows) =>
        new(true, rows, rows.Count, null);

    public static SqlExecutionResult Fail(string error) =>
        new(false, null, 0, error);
}
