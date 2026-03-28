using FluentAssertions;
using Sentinel.Api.Gates;
using Sentinel.Api.Models;
using Sentinel.Api.Parsing;

namespace Sentinel.Tests.Gates;

public class LexicalGateTests
{
    private readonly LexicalGate _gate = new(new SqlGuard());

    [Fact]
    public async Task ValidSelect_ShouldPass()
    {
        var context = new QueryContext { UserPrompt = "test" };
        context.GeneratedSql = "SELECT * FROM Orders";

        var result = await _gate.EvaluateAsync(context);

        result.Passed.Should().BeTrue();
        result.GateName.Should().Be("G1-LEXICAL");
    }

    [Fact]
    public async Task DeleteStatement_ShouldFail()
    {
        var context = new QueryContext { UserPrompt = "test" };
        context.GeneratedSql = "DELETE FROM Orders";

        var result = await _gate.EvaluateAsync(context);

        result.Passed.Should().BeFalse();
        result.GateName.Should().Be("G1-LEXICAL");
        result.Reason.Should().Contain("DeleteStatement");
    }

    [Fact]
    public async Task NullSql_ShouldFail()
    {
        var context = new QueryContext { UserPrompt = "test" };
        context.GeneratedSql = null;

        var result = await _gate.EvaluateAsync(context);

        result.Passed.Should().BeFalse();
    }
}
