using FluentAssertions;
using Moq;
using Sentinel.Api.Gates;
using Sentinel.Api.Models;
using Sentinel.Api.Services;

namespace Sentinel.Tests.Gates;

public class SemanticGateTests
{
    [Fact]
    public async Task MatchingIntent_ShouldPass()
    {
        var mockLlm = new Mock<ILlmService>();
        mockLlm.Setup(x => x.VerifyIntentAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var gate = new SemanticGate(mockLlm.Object);
        var context = new QueryContext { UserPrompt = "Show all orders" };
        context.GeneratedSql = "SELECT * FROM Orders";

        var result = await gate.EvaluateAsync(context);
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public async Task MismatchedIntent_ShouldFail()
    {
        var mockLlm = new Mock<ILlmService>();
        mockLlm.Setup(x => x.VerifyIntentAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var gate = new SemanticGate(mockLlm.Object);
        var context = new QueryContext { UserPrompt = "Show all orders" };
        context.GeneratedSql = "SELECT * FROM Products";

        var result = await gate.EvaluateAsync(context);

        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain("does not appear to match");
    }

    [Fact]
    public async Task NullSql_ShouldFail()
    {
        var mockLlm = new Mock<ILlmService>();
        var gate = new SemanticGate(mockLlm.Object);
        var context = new QueryContext { UserPrompt = "test" };

        var result = await gate.EvaluateAsync(context);
        result.Passed.Should().BeFalse();
    }
}
