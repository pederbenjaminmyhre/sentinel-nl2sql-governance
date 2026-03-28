using FluentAssertions;
using Moq;
using Sentinel.Api.Gates;
using Sentinel.Api.Models;
using Sentinel.Api.Services;

namespace Sentinel.Tests.Gates;

public class SandboxGateTests
{
    [Fact]
    public async Task SuccessfulExecution_ShouldPass()
    {
        var mockExecutor = new Mock<ISqlExecutor>();
        mockExecutor.Setup(x => x.ExecuteAsync(It.IsAny<string>()))
            .ReturnsAsync(SqlExecutionResult.Ok(
            [
                new Dictionary<string, object?> { ["OrderId"] = 1, ["Total"] = 99.99m }
            ]));

        var gate = new SandboxGate(mockExecutor.Object);
        var context = new QueryContext { UserPrompt = "test" };
        context.GeneratedSql = "SELECT * FROM Orders";

        var result = await gate.EvaluateAsync(context);

        result.Passed.Should().BeTrue();
        context.ExecutionResult.Should().NotBeNull();
        context.ExecutionResult!.RowCount.Should().Be(1);
    }

    [Fact]
    public async Task TimeoutExecution_ShouldFail()
    {
        var mockExecutor = new Mock<ISqlExecutor>();
        mockExecutor.Setup(x => x.ExecuteAsync(It.IsAny<string>()))
            .ReturnsAsync(SqlExecutionResult.Fail("Query exceeded 30-second timeout."));

        var gate = new SandboxGate(mockExecutor.Object);
        var context = new QueryContext { UserPrompt = "test" };
        context.GeneratedSql = "SELECT * FROM Orders";

        var result = await gate.EvaluateAsync(context);

        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain("timeout");
    }

    [Fact]
    public async Task NullSql_ShouldFail()
    {
        var mockExecutor = new Mock<ISqlExecutor>();
        var gate = new SandboxGate(mockExecutor.Object);
        var context = new QueryContext { UserPrompt = "test" };

        var result = await gate.EvaluateAsync(context);
        result.Passed.Should().BeFalse();
    }
}
