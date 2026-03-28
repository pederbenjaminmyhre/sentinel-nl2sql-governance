using Sentinel.Api.Configuration;

namespace Sentinel.Api.Services;

public interface ILlmService
{
    Task<string> GenerateSqlAsync(string naturalLanguagePrompt, SafeSchema schema);
    Task<bool> VerifyIntentAsync(string originalPrompt, string generatedSql);
    Task<string> CorrectSqlAsync(string sql, string error);
}
