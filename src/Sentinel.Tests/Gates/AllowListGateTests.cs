using FluentAssertions;
using Microsoft.Extensions.Options;
using Sentinel.Api.Configuration;
using Sentinel.Api.Gates;
using Sentinel.Api.Models;
using Sentinel.Api.Parsing;

namespace Sentinel.Tests.Gates;

public class AllowListGateTests
{
    private readonly AllowListGate _gate;

    public AllowListGateTests()
    {
        var schema = new SafeSchema
        {
            AllowedObjects =
            [
                new AllowedObject
                {
                    Schema = "dbo",
                    Table = "Orders",
                    Columns = ["OrderId", "CustomerId", "OrderDate", "TotalAmount", "Status"]
                },
                new AllowedObject
                {
                    Schema = "dbo",
                    Table = "Customers",
                    Columns = ["CustomerId", "FirstName", "LastName", "Email"]
                }
            ]
        };

        _gate = new AllowListGate(new SqlGuard(), new SchemaExtractor(), Options.Create(schema));
    }

    [Fact]
    public async Task AllowedTable_ShouldPass()
    {
        var context = new QueryContext { UserPrompt = "test" };
        context.GeneratedSql = "SELECT OrderId, TotalAmount FROM dbo.Orders";

        var result = await _gate.EvaluateAsync(context);
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public async Task UnauthorizedTable_ShouldFail()
    {
        var context = new QueryContext { UserPrompt = "test" };
        context.GeneratedSql = "SELECT * FROM dbo.Salary";

        var result = await _gate.EvaluateAsync(context);

        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain("Salary");
        result.Reason.Should().Contain("not in the approved schema");
    }

    [Fact]
    public async Task JoinWithUnauthorizedTable_ShouldFail()
    {
        var context = new QueryContext { UserPrompt = "test" };
        context.GeneratedSql = "SELECT o.OrderId FROM dbo.Orders o JOIN dbo.UserCredentials u ON o.CustomerId = u.UserId";

        var result = await _gate.EvaluateAsync(context);

        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain("UserCredentials");
    }

    [Fact]
    public async Task AllowedJoin_ShouldPass()
    {
        var context = new QueryContext { UserPrompt = "test" };
        context.GeneratedSql = "SELECT o.OrderId, c.FirstName FROM dbo.Orders o JOIN dbo.Customers c ON o.CustomerId = c.CustomerId";

        var result = await _gate.EvaluateAsync(context);
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public async Task UnauthorizedColumn_ShouldFail()
    {
        var context = new QueryContext { UserPrompt = "test" };
        context.GeneratedSql = "SELECT Orders.Salary FROM dbo.Orders";

        var result = await _gate.EvaluateAsync(context);

        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain("Salary");
    }
}
