using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Sentinel.Api.Parsing;

public class SchemaExtractor
{
    public SchemaReferences Extract(TSqlFragment fragment)
    {
        var visitor = new SchemaVisitor();
        fragment.Accept(visitor);
        return new SchemaReferences(visitor.Tables, visitor.Columns);
    }

    private sealed class SchemaVisitor : TSqlFragmentVisitor
    {
        public HashSet<TableReference> Tables { get; } = [];
        public HashSet<ColumnReference> Columns { get; } = [];

        public override void Visit(NamedTableReference node)
        {
            var schema = node.SchemaObject.SchemaIdentifier?.Value ?? "dbo";
            var table = node.SchemaObject.BaseIdentifier.Value;
            Tables.Add(new TableReference(schema, table));
        }

        public override void Visit(ColumnReferenceExpression node)
        {
            if (node.MultiPartIdentifier?.Identifiers is { Count: > 0 } identifiers)
            {
                var columnName = identifiers[^1].Value;
                string? tableName = identifiers.Count >= 2 ? identifiers[^2].Value : null;
                Columns.Add(new ColumnReference(tableName, columnName));
            }
        }
    }
}

public record SchemaReferences(
    HashSet<TableReference> Tables,
    HashSet<ColumnReference> Columns
);

public record TableReference(string Schema, string Table);
public record ColumnReference(string? TableOrAlias, string Column);
