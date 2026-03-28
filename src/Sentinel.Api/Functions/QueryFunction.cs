using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentinel.Api.Configuration;
using Sentinel.Api.Gates;
using Sentinel.Api.Models;
using Sentinel.Api.Parsing;
using Sentinel.Api.Services;

namespace Sentinel.Api.Functions;

public class QueryFunction(
    ILlmService llmService,
    SqlGuard sqlGuard,
    GatePipeline pipeline,
    AuditLogger auditLogger,
    IOptions<SafeSchema> schema,
    IOptions<SentinelOptions> options,
    ILogger<QueryFunction> logger)
{
    [Function("Query")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "query")] HttpRequest req)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            return await ProcessQueryAsync(req, sw);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in QueryFunction");
            return new ObjectResult(new { error = ex.Message, type = ex.GetType().Name })
                { StatusCode = 500 };
        }
    }

    private async Task<IActionResult> ProcessQueryAsync(HttpRequest req, Stopwatch sw)
    {
        QueryRequest? request;
        try
        {
            request = await req.ReadFromJsonAsync<QueryRequest>();
        }
        catch
        {
            return new BadRequestObjectResult(new { error = "Invalid JSON body. Expected: { \"prompt\": \"...\" }" });
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Prompt))
            return new BadRequestObjectResult(new { error = "A non-empty 'prompt' field is required." });

        var context = new QueryContext { UserPrompt = request.Prompt };

        var sql = await GenerateWithCorrectionAsync(context);
        if (sql is null)
        {
            context.ElapsedMs = sw.Elapsed.TotalMilliseconds;
            auditLogger.Log(context);
            return new UnprocessableEntityObjectResult(BuildResponse(context));
        }

        context.GeneratedSql = sql;

        var passed = await pipeline.ExecuteAsync(context);
        context.ElapsedMs = sw.Elapsed.TotalMilliseconds;
        auditLogger.Log(context);

        if (!passed)
            return new UnprocessableEntityObjectResult(BuildResponse(context));

        return new OkObjectResult(BuildResponse(context));
    }

    private async Task<string?> GenerateWithCorrectionAsync(QueryContext context)
    {
        var maxRetries = options.Value.MaxCorrectionRetries;
        var sql = await llmService.GenerateSqlAsync(context.UserPrompt, schema.Value);

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            var guardResult = sqlGuard.Evaluate(sql);
            if (guardResult.IsAllowed)
                return sql;

            if (attempt == maxRetries)
            {
                context.GeneratedSql = sql;
                context.GateResults.Add(GateResult.Fail("G1-CORRECTION",
                    $"Failed after {maxRetries + 1} attempts: {guardResult.Reason}"));
                return null;
            }

            logger.LogWarning("Correction attempt {Attempt}: {Reason}", attempt + 1, guardResult.Reason);
            sql = await llmService.CorrectSqlAsync(sql, guardResult.Reason!);
        }

        return null;
    }

    private static QueryResponse BuildResponse(QueryContext context)
    {
        return new QueryResponse(
            Success: context.ExecutionResult?.Success == true,
            GeneratedSql: context.GeneratedSql,
            Results: context.ExecutionResult?.Rows,
            Audit: new AuditSummary(
                GateResults: context.GateResults.Select(g => g.ToString()).ToArray(),
                ElapsedMs: context.ElapsedMs
            )
        );
    }
}
