using FluentAssertions;
using Sentinel.Api.Services;

namespace Sentinel.Tests.Services;

public class SqlExecutorTests
{
    [Fact]
    public void InjectRowLimit_AddsTop_WhenNotPresent()
    {
        var result = SqlExecutor.InjectRowLimit("SELECT * FROM Orders", 1000);
        result.Should().StartWith("SELECT TOP 1000");
        result.Should().Contain("FROM Orders");
    }

    [Fact]
    public void InjectRowLimit_PreservesExistingTop()
    {
        var sql = "SELECT TOP 10 * FROM Orders";
        var result = SqlExecutor.InjectRowLimit(sql, 1000);
        result.Should().Be(sql);
    }

    [Fact]
    public void InjectRowLimit_HandlesLeadingWhitespace()
    {
        var result = SqlExecutor.InjectRowLimit("  SELECT OrderId FROM Orders", 500);
        result.Should().StartWith("SELECT TOP 500");
    }

    [Fact]
    public void InjectRowLimit_CaseInsensitive()
    {
        var result = SqlExecutor.InjectRowLimit("select * from Orders", 1000);
        result.Should().StartWith("SELECT TOP 1000");
    }
}
