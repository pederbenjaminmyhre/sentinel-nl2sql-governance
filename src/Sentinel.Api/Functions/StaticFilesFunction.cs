using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Sentinel.Api.Functions;

public class StaticFilesFunction
{
    [Function("ServeDemo")]
    public async Task<IActionResult> ServeDemo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "demo")] HttpRequest req)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "wwwroot", "index.html");

        if (!File.Exists(path))
            return new NotFoundResult();

        var content = await File.ReadAllTextAsync(path);
        return new ContentResult { Content = content, ContentType = "text/html", StatusCode = 200 };
    }
}
