using Sentinel.Api.Models;

namespace Sentinel.Api.Gates;

public interface IGate
{
    string Name { get; }
    Task<GateResult> EvaluateAsync(QueryContext context);
}
