using Sentinel.Api.Configuration;

namespace Sentinel.Api.Services;

public class DemoLlmService : ILlmService
{
    private static readonly Dictionary<string, string> _promptToSql = new(StringComparer.OrdinalIgnoreCase)
    {
        ["show me all orders"] = "SELECT OrderId, CustomerId, OrderDate, TotalAmount, Status FROM dbo.Orders",
        ["show me all orders from last month"] = "SELECT OrderId, CustomerId, OrderDate, TotalAmount, Status FROM dbo.Orders WHERE OrderDate >= DATEADD(MONTH, -1, GETDATE())",
        ["how many orders are there"] = "SELECT COUNT(*) AS OrderCount FROM dbo.Orders",
        ["list all customers"] = "SELECT CustomerId, FirstName, LastName, Email, City, State FROM dbo.Customers",
        ["show me all products"] = "SELECT ProductId, Name, Category, Price, StockQuantity FROM dbo.Products",
        ["top 5 customers by order total"] = "SELECT TOP 5 c.FirstName, c.LastName, SUM(o.TotalAmount) AS TotalSpent FROM dbo.Orders o JOIN dbo.Customers c ON o.CustomerId = c.CustomerId GROUP BY c.FirstName, c.LastName ORDER BY TotalSpent DESC",
        ["show order items with product names"] = "SELECT oi.OrderItemId, p.Name AS ProductName, oi.Quantity, oi.UnitPrice FROM dbo.OrderItems oi JOIN dbo.Products p ON oi.ProductId = p.ProductId",
        ["average order value"] = "SELECT AVG(TotalAmount) AS AvgOrderValue FROM dbo.Orders",

        // Intentional gate-failure demos (simulates a misbehaving LLM)
        ["delete all orders"] = "DELETE FROM dbo.Orders",
        ["drop the users table"] = "DROP TABLE dbo.Users",
        ["update order status"] = "UPDATE dbo.Orders SET Status = 'Cancelled' WHERE OrderId = 1",
        ["show me salaries"] = "SELECT * FROM dbo.Salary",
        ["show user credentials"] = "SELECT * FROM dbo.UserCredentials",
    };

    public Task<string> GenerateSqlAsync(string naturalLanguagePrompt, SafeSchema schema)
    {
        // Try exact match first, then partial match
        if (_promptToSql.TryGetValue(naturalLanguagePrompt.Trim(), out var sql))
            return Task.FromResult(sql);

        foreach (var kvp in _promptToSql)
        {
            if (naturalLanguagePrompt.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(kvp.Value);
        }

        // Default: return a safe generic query
        return Task.FromResult("SELECT TOP 10 OrderId, CustomerId, OrderDate, TotalAmount FROM dbo.Orders ORDER BY OrderDate DESC");
    }

    public Task<bool> VerifyIntentAsync(string originalPrompt, string generatedSql)
    {
        // In demo mode, always pass semantic verification
        return Task.FromResult(true);
    }

    public Task<string> CorrectSqlAsync(string sql, string error)
    {
        // In demo mode, return the same SQL unchanged to demonstrate gate blocking
        return Task.FromResult(sql);
    }
}
