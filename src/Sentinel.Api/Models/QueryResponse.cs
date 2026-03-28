namespace Sentinel.Api.Models;

public record QueryResponse(
    bool Success,
    string? GeneratedSql,
    object? Results,
    AuditSummary Audit
);

public record AuditSummary(
    string[] GateResults,
    double ElapsedMs
);
