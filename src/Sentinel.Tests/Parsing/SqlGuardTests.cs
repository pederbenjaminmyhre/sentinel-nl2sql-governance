using FluentAssertions;
using Sentinel.Api.Parsing;

namespace Sentinel.Tests.Parsing;

public class SqlGuardTests
{
    private readonly SqlGuard _guard = new();

    [Fact]
    public void SimpleSelect_ShouldBeAllowed()
    {
        var result = _guard.Evaluate("SELECT * FROM Orders");
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void SelectWithJoin_ShouldBeAllowed()
    {
        var result = _guard.Evaluate(
            "SELECT o.OrderId, c.FirstName FROM Orders o JOIN Customers c ON o.CustomerId = c.CustomerId");
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void SelectWithCte_ShouldBeAllowed()
    {
        var result = _guard.Evaluate(
            "WITH cte AS (SELECT OrderId, TotalAmount FROM Orders WHERE TotalAmount > 100) SELECT * FROM cte");
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void SelectWithWhereClause_ShouldBeAllowed()
    {
        var result = _guard.Evaluate("SELECT OrderId FROM Orders WHERE Status = 'Shipped'");
        result.IsAllowed.Should().BeTrue();
    }

    [Theory]
    [InlineData("DELETE FROM Orders", "DeleteStatement")]
    [InlineData("DROP TABLE Orders", "DropTableStatement")]
    [InlineData("TRUNCATE TABLE Orders", "TruncateTableStatement")]
    [InlineData("UPDATE Orders SET Status = 'Cancelled'", "UpdateStatement")]
    [InlineData("INSERT INTO Orders (CustomerId) VALUES (1)", "InsertStatement")]
    [InlineData("CREATE TABLE Evil (Id INT)", "CreateTableStatement")]
    [InlineData("ALTER TABLE Orders ADD EvilColumn INT", "AlterTableAddTableElementStatement")]
    [InlineData("EXEC sp_executesql N'SELECT 1'", "ExecuteStatement")]
    public void DangerousStatements_ShouldBeBlocked(string sql, string expectedType)
    {
        var result = _guard.Evaluate(sql);
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Contain(expectedType);
    }

    [Fact]
    public void BatchWithHiddenDdl_ShouldBeBlocked()
    {
        var result = _guard.Evaluate("SELECT 1; DROP TABLE Users");
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Contain("DropTableStatement");
    }

    [Fact]
    public void CommentObfuscatedDrop_ShouldBeBlocked()
    {
        var result = _guard.Evaluate("/* comment */ DROP /* comment */ TABLE Users");
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void NullInput_ShouldBeBlocked()
    {
        var result = _guard.Evaluate(null);
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Contain("null or empty");
    }

    [Fact]
    public void EmptyInput_ShouldBeBlocked()
    {
        var result = _guard.Evaluate("");
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void WhitespaceInput_ShouldBeBlocked()
    {
        var result = _guard.Evaluate("   ");
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void SelectTopN_ShouldBeAllowed()
    {
        var result = _guard.Evaluate("SELECT TOP 10 * FROM Products");
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void SelectWithSubquery_ShouldBeAllowed()
    {
        var result = _guard.Evaluate(
            "SELECT * FROM Orders WHERE CustomerId IN (SELECT CustomerId FROM Customers WHERE City = 'Seattle')");
        result.IsAllowed.Should().BeTrue();
    }
}
