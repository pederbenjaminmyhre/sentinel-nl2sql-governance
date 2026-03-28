namespace Sentinel.Api.Models;

public record AuditEntry(
    string UserPrompt,
    string? GeneratedSql,
    List<GateResult> GateResults,
    bool OverallPass,
    double ElapsedMs,
    DateTime Timestamp
);
