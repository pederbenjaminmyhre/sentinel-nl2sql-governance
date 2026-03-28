using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sentinel.Api.Configuration;
using Sentinel.Api.Gates;
using Sentinel.Api.Parsing;
using Sentinel.Api.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

var isDemoMode = builder.Configuration.GetValue<bool>("Sentinel:DemoMode");

// Configuration
builder.Services.Configure<SentinelOptions>(
    builder.Configuration.GetSection("Sentinel"));
builder.Services.Configure<SafeSchema>(
    builder.Configuration.GetSection("SafeSchema"));

// Parsing
builder.Services.AddSingleton<SqlGuard>();
builder.Services.AddSingleton<SchemaExtractor>();

if (isDemoMode)
{
    // Demo mode: in-memory mocks, no external dependencies
    builder.Services.AddSingleton<ILlmService, DemoLlmService>();
    builder.Services.AddSingleton<ISqlExecutor, DemoSqlExecutor>();
}
else
{
    // Production: Azure OpenAI + SQL Server
    builder.Services.AddSingleton(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var endpoint = config["AzureOpenAi:Endpoint"];

        if (!string.IsNullOrEmpty(endpoint))
        {
            var apiKey = config["AzureOpenAi:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
                return new AzureOpenAIClient(new Uri(endpoint), new System.ClientModel.ApiKeyCredential(apiKey));

            return new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
        }

        throw new InvalidOperationException(
            "AzureOpenAi:Endpoint must be configured. Set Sentinel:DemoMode=true to run without Azure OpenAI.");
    });
    builder.Services.AddSingleton<ILlmService, AzureOpenAiService>();
    builder.Services.AddSingleton<ISqlExecutor, SqlExecutor>();
}

// Audit
builder.Services.AddSingleton<AuditLogger>();

// Gates (registered in pipeline execution order)
builder.Services.AddSingleton<IGate, LexicalGate>();
builder.Services.AddSingleton<IGate, AllowListGate>();
builder.Services.AddSingleton<IGate, SemanticGate>();
builder.Services.AddSingleton<IGate, SandboxGate>();
builder.Services.AddSingleton<GatePipeline>();

// Application Insights
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
