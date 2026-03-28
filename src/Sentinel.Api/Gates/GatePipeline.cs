using Sentinel.Api.Models;

namespace Sentinel.Api.Gates;

public class GatePipeline(IEnumerable<IGate> gates)
{
    private readonly IGate[] _gates = gates.ToArray();

    public async Task<bool> ExecuteAsync(QueryContext context)
    {
        foreach (var gate in _gates)
        {
            var result = await gate.EvaluateAsync(context);
            context.GateResults.Add(result);

            if (!result.Passed)
                return false;
        }

        return true;
    }
}
