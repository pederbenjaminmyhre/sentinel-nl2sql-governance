using System.Text.Json;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using Sentinel.Api.Configuration;

namespace Sentinel.Api.Services;

public class AzureOpenAiService : ILlmService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<AzureOpenAiService> _logger;

    public AzureOpenAiService(AzureOpenAIClient openAiClient, IConfiguration config, ILogger<AzureOpenAiService> logger)
    {
        var deploymentName = config["AzureOpenAi:DeploymentName"] ?? "gpt-4o";
        _chatClient = openAiClient.GetChatClient(deploymentName);
        _logger = logger;
    }

    public async Task<string> GenerateSqlAsync(string naturalLanguagePrompt, SafeSchema schema)
    {
        var schemaDescription = BuildSchemaDescription(schema);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                $"""
                You are a T-SQL query generator. You MUST generate ONLY SELECT statements.
                Never generate DELETE, DROP, UPDATE, INSERT, TRUNCATE, EXECUTE, or any DDL/DML.

                Available schema:
                {schemaDescription}

                Rules:
                - Only reference tables and columns listed above.
                - Always use schema-qualified names (e.g., dbo.Orders).
                - Return ONLY the SQL query, no explanation or markdown.
                """),
            new UserChatMessage(naturalLanguagePrompt)
        };

        var response = await _chatClient.CompleteChatAsync(messages);
        var sql = response.Value.Content[0].Text.Trim();

        _logger.LogInformation("Generated SQL for prompt: {Prompt} -> {Sql}", naturalLanguagePrompt, sql);
        return sql;
    }

    public async Task<bool> VerifyIntentAsync(string originalPrompt, string generatedSql)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                """
                You are a SQL verification assistant. Compare the user's original question
                with the generated SQL query. Determine if the SQL correctly answers the question.
                Respond with ONLY "YES" or "NO" followed by a brief reason on the same line.
                """),
            new UserChatMessage(
                $"""
                Original question: {originalPrompt}
                Generated SQL: {generatedSql}
                Does this SQL correctly answer the question?
                """)
        };

        var response = await _chatClient.CompleteChatAsync(messages);
        var answer = response.Value.Content[0].Text.Trim();

        _logger.LogInformation("Semantic verification: {Answer}", answer);
        return answer.StartsWith("YES", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> CorrectSqlAsync(string sql, string error)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                """
                You are a T-SQL correction assistant. Fix the SQL query based on the error.
                Return ONLY the corrected SQL query, no explanation or markdown.
                You MUST only generate SELECT statements.
                """),
            new UserChatMessage(
                $"""
                SQL with error:
                {sql}

                Error:
                {error}

                Provide the corrected SQL:
                """)
        };

        var response = await _chatClient.CompleteChatAsync(messages);
        return response.Value.Content[0].Text.Trim();
    }

    private static string BuildSchemaDescription(SafeSchema schema)
    {
        return string.Join("\n", schema.AllowedObjects.Select(o =>
            $"  {o.Schema}.{o.Table} ({string.Join(", ", o.Columns)})"));
    }
}
