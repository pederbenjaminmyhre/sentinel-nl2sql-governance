namespace Sentinel.Api.Models;

public record GateResult(string GateName, bool Passed, string? Reason = null)
{
    public static GateResult Pass(string gateName) => new(gateName, true);
    public static GateResult Fail(string gateName, string reason) => new(gateName, false, reason);

    public override string ToString() => $"{GateName}:{(Passed ? "PASS" : "FAIL")}{(Reason is not null ? $" ({Reason})" : "")}";
}
