using Sentinel.Api.Models;
using Sentinel.Api.Services;

namespace Sentinel.Api.Gates;

public class SandboxGate(ISqlExecutor executor) : IGate
{
    public string Name => "G4-SANDBOX";

    public async Task<GateResult> EvaluateAsync(QueryContext context)
    {
        if (string.IsNullOrWhiteSpace(context.GeneratedSql))
            return GateResult.Fail(Name, "No SQL to execute.");

        var result = await executor.ExecuteAsync(context.GeneratedSql);

        if (!result.Success)
            return GateResult.Fail(Name, result.Error!);

        context.ExecutionResult = result;
        return GateResult.Pass(Name);
    }
}
