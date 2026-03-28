using Sentinel.Api.Models;
using Sentinel.Api.Parsing;

namespace Sentinel.Api.Gates;

public class LexicalGate(SqlGuard sqlGuard) : IGate
{
    public string Name => "G1-LEXICAL";

    public Task<GateResult> EvaluateAsync(QueryContext context)
    {
        var result = sqlGuard.Evaluate(context.GeneratedSql);

        if (result.IsAllowed)
            return Task.FromResult(GateResult.Pass(Name));

        return Task.FromResult(GateResult.Fail(Name, result.Reason!));
    }
}
