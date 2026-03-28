using Microsoft.Extensions.Options;
using Sentinel.Api.Configuration;
using Sentinel.Api.Models;
using Sentinel.Api.Parsing;

namespace Sentinel.Api.Gates;

public class AllowListGate(SqlGuard sqlGuard, SchemaExtractor extractor, IOptions<SafeSchema> schema) : IGate
{
    public string Name => "G2-ALLOWLIST";

    public Task<GateResult> EvaluateAsync(QueryContext context)
    {
        var guardResult = sqlGuard.Evaluate(context.GeneratedSql);
        if (!guardResult.IsAllowed || guardResult.ParsedFragment is null)
            return Task.FromResult(GateResult.Fail(Name, "Cannot extract schema from unparsable SQL."));

        var refs = extractor.Extract(guardResult.ParsedFragment);
        var allowed = schema.Value.AllowedObjects;

        foreach (var tableRef in refs.Tables)
        {
            var match = allowed.FirstOrDefault(a =>
                string.Equals(a.Schema, tableRef.Schema, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(a.Table, tableRef.Table, StringComparison.OrdinalIgnoreCase));

            if (match is null)
                return Task.FromResult(GateResult.Fail(Name,
                    $"Table '{tableRef.Schema}.{tableRef.Table}' is not in the approved schema."));
        }

        foreach (var colRef in refs.Columns)
        {
            if (colRef.TableOrAlias is null)
                continue;

            var tableMatch = allowed.FirstOrDefault(a =>
                string.Equals(a.Table, colRef.TableOrAlias, StringComparison.OrdinalIgnoreCase));

            if (tableMatch is not null &&
                !tableMatch.Columns.Any(c => string.Equals(c, colRef.Column, StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult(GateResult.Fail(Name,
                    $"Column '{colRef.Column}' on table '{colRef.TableOrAlias}' is not in the approved schema."));
            }
        }

        return Task.FromResult(GateResult.Pass(Name));
    }
}
