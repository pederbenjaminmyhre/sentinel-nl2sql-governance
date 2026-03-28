using Sentinel.Api.Models;
using Sentinel.Api.Services;

namespace Sentinel.Api.Gates;

public class SemanticGate(ILlmService llmService) : IGate
{
    public string Name => "G3-SEMANTIC";

    public async Task<GateResult> EvaluateAsync(QueryContext context)
    {
        if (string.IsNullOrWhiteSpace(context.GeneratedSql))
            return GateResult.Fail(Name, "No SQL to verify.");

        var isAligned = await llmService.VerifyIntentAsync(context.UserPrompt, context.GeneratedSql);

        return isAligned
            ? GateResult.Pass(Name)
            : GateResult.Fail(Name, "Generated SQL does not appear to match the user's intent.");
    }
}
