using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Sentinel.Api.Parsing;

public class SqlGuard
{
    private static readonly HashSet<Type> _blockedStatements =
    [
        typeof(DeleteStatement),
        typeof(DropTableStatement),
        typeof(DropViewStatement),
        typeof(DropProcedureStatement),
        typeof(DropFunctionStatement),
        typeof(DropDatabaseStatement),
        typeof(TruncateTableStatement),
        typeof(UpdateStatement),
        typeof(InsertStatement),
        typeof(MergeStatement),
        typeof(CreateTableStatement),
        typeof(CreateViewStatement),
        typeof(CreateProcedureStatement),
        typeof(CreateFunctionStatement),
        typeof(AlterTableStatement),
        typeof(AlterTableAddTableElementStatement),
        typeof(AlterViewStatement),
        typeof(AlterProcedureStatement),
        typeof(AlterFunctionStatement),
        typeof(ExecuteStatement),
        typeof(GrantStatement),
        typeof(DenyStatement),
        typeof(RevokeStatement),
    ];

    public SqlGuardResult Evaluate(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return SqlGuardResult.Blocked("Input SQL is null or empty.");

        var parser = new TSql160Parser(initialQuotedIdentifiers: false);
        using var reader = new StringReader(sql);
        var fragment = parser.Parse(reader, out var errors);

        if (errors.Count > 0)
        {
            var errorMessages = string.Join("; ", errors.Select(e => $"Line {e.Line}: {e.Message}"));
            return SqlGuardResult.Blocked($"SQL parse errors: {errorMessages}");
        }

        if (fragment is not TSqlScript script)
            return SqlGuardResult.Blocked("Could not parse as T-SQL script.");

        if (script.Batches.Count == 0)
            return SqlGuardResult.Blocked("No SQL batches found.");

        foreach (var batch in script.Batches)
        {
            foreach (var statement in batch.Statements)
            {
                var statementType = statement.GetType();
                if (_blockedStatements.Contains(statementType))
                {
                    return SqlGuardResult.Blocked(
                        $"Blocked statement type: {statementType.Name}");
                }

                if (statement is not SelectStatement)
                {
                    return SqlGuardResult.Blocked(
                        $"Only SELECT statements are allowed. Found: {statementType.Name}");
                }
            }
        }

        return SqlGuardResult.Allowed(fragment);
    }
}

public record SqlGuardResult(bool IsAllowed, string? Reason, TSqlFragment? ParsedFragment)
{
    public static SqlGuardResult Allowed(TSqlFragment fragment) => new(true, null, fragment);
    public static SqlGuardResult Blocked(string reason) => new(false, reason, null);
}
