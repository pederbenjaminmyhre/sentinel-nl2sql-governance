using FluentAssertions;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Sentinel.Api.Parsing;
using TableRef = Sentinel.Api.Parsing.TableReference;

namespace Sentinel.Tests.Parsing;

public class SchemaExtractorTests
{
    private readonly SchemaExtractor _extractor = new();

    private static TSqlFragment Parse(string sql)
    {
        var parser = new TSql160Parser(initialQuotedIdentifiers: false);
        using var reader = new StringReader(sql);
        var fragment = parser.Parse(reader, out _);
        return fragment;
    }

    [Fact]
    public void ExtractsTables_FromSimpleSelect()
    {
        var fragment = Parse("SELECT * FROM dbo.Orders");
        var refs = _extractor.Extract(fragment);

        refs.Tables.Should().Contain(new TableRef("dbo", "Orders"));
    }

    [Fact]
    public void ExtractsTables_FromJoin()
    {
        var fragment = Parse(
            "SELECT o.OrderId FROM dbo.Orders o JOIN dbo.Customers c ON o.CustomerId = c.CustomerId");
        var refs = _extractor.Extract(fragment);

        refs.Tables.Should().HaveCount(2);
        refs.Tables.Should().Contain(new TableRef("dbo", "Orders"));
        refs.Tables.Should().Contain(new TableRef("dbo", "Customers"));
    }

    [Fact]
    public void DefaultsSchema_ToDbo_WhenNotSpecified()
    {
        var fragment = Parse("SELECT * FROM Orders");
        var refs = _extractor.Extract(fragment);

        refs.Tables.Should().Contain(new TableRef("dbo", "Orders"));
    }

    [Fact]
    public void ExtractsColumns_WithTableAlias()
    {
        var fragment = Parse("SELECT o.OrderId, o.TotalAmount FROM Orders o");
        var refs = _extractor.Extract(fragment);

        refs.Columns.Should().Contain(new ColumnReference("o", "OrderId"));
        refs.Columns.Should().Contain(new ColumnReference("o", "TotalAmount"));
    }
}
