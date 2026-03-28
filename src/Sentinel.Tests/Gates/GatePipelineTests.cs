using FluentAssertions;
using Moq;
using Sentinel.Api.Gates;
using Sentinel.Api.Models;

namespace Sentinel.Tests.Gates;

public class GatePipelineTests
{
    [Fact]
    public async Task AllGatesPass_ShouldReturnTrue()
    {
        var gate1 = CreateMockGate("G1", true);
        var gate2 = CreateMockGate("G2", true);

        var pipeline = new GatePipeline([gate1.Object, gate2.Object]);
        var context = new QueryContext { UserPrompt = "test" };

        var result = await pipeline.ExecuteAsync(context);

        result.Should().BeTrue();
        context.GateResults.Should().HaveCount(2);
        context.GateResults.Should().AllSatisfy(g => g.Passed.Should().BeTrue());
    }

    [Fact]
    public async Task FirstGateFails_ShouldShortCircuit()
    {
        var gate1 = CreateMockGate("G1", false, "blocked");
        var gate2 = CreateMockGate("G2", true);

        var pipeline = new GatePipeline([gate1.Object, gate2.Object]);
        var context = new QueryContext { UserPrompt = "test" };

        var result = await pipeline.ExecuteAsync(context);

        result.Should().BeFalse();
        context.GateResults.Should().HaveCount(1);
        gate2.Verify(g => g.EvaluateAsync(It.IsAny<QueryContext>()), Times.Never);
    }

    [Fact]
    public async Task SecondGateFails_ShouldStopAtSecond()
    {
        var gate1 = CreateMockGate("G1", true);
        var gate2 = CreateMockGate("G2", false, "not allowed");
        var gate3 = CreateMockGate("G3", true);

        var pipeline = new GatePipeline([gate1.Object, gate2.Object, gate3.Object]);
        var context = new QueryContext { UserPrompt = "test" };

        var result = await pipeline.ExecuteAsync(context);

        result.Should().BeFalse();
        context.GateResults.Should().HaveCount(2);
        gate3.Verify(g => g.EvaluateAsync(It.IsAny<QueryContext>()), Times.Never);
    }

    private static Mock<IGate> CreateMockGate(string name, bool passes, string? reason = null)
    {
        var mock = new Mock<IGate>();
        mock.Setup(g => g.Name).Returns(name);
        mock.Setup(g => g.EvaluateAsync(It.IsAny<QueryContext>()))
            .ReturnsAsync(passes ? GateResult.Pass(name) : GateResult.Fail(name, reason!));
        return mock;
    }
}
