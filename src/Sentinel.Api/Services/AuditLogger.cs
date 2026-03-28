using Microsoft.Extensions.Logging;
using Sentinel.Api.Models;

namespace Sentinel.Api.Services;

public class AuditLogger(ILogger<AuditLogger> logger)
{
    public void Log(QueryContext context)
    {
        var entry = new AuditEntry(
            UserPrompt: context.UserPrompt,
            GeneratedSql: context.GeneratedSql,
            GateResults: context.GateResults,
            OverallPass: context.GateResults.All(g => g.Passed),
            ElapsedMs: context.ElapsedMs,
            Timestamp: context.Timestamp
        );

        if (entry.OverallPass)
        {
            logger.LogInformation(
                "AUDIT PASS | Prompt: {Prompt} | SQL: {Sql} | Gates: {Gates} | Elapsed: {Elapsed}ms",
                entry.UserPrompt,
                entry.GeneratedSql,
                string.Join(", ", entry.GateResults.Select(g => g.ToString())),
                entry.ElapsedMs);
        }
        else
        {
            var failedGate = entry.GateResults.FirstOrDefault(g => !g.Passed);
            logger.LogWarning(
                "AUDIT FAIL | Prompt: {Prompt} | SQL: {Sql} | FailedGate: {Gate} | Reason: {Reason} | Elapsed: {Elapsed}ms",
                entry.UserPrompt,
                entry.GeneratedSql,
                failedGate?.GateName,
                failedGate?.Reason,
                entry.ElapsedMs);
        }
    }
}
