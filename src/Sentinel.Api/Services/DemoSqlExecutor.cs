namespace Sentinel.Api.Services;

public class DemoSqlExecutor : ISqlExecutor
{
    private static readonly List<Dictionary<string, object?>> _orders =
    [
        new() { ["OrderId"] = 1001, ["CustomerId"] = 42, ["OrderDate"] = "2026-03-15", ["TotalAmount"] = 299.99m, ["Status"] = "Shipped" },
        new() { ["OrderId"] = 1002, ["CustomerId"] = 17, ["OrderDate"] = "2026-03-18", ["TotalAmount"] = 149.50m, ["Status"] = "Processing" },
        new() { ["OrderId"] = 1003, ["CustomerId"] = 42, ["OrderDate"] = "2026-03-20", ["TotalAmount"] = 89.00m, ["Status"] = "Delivered" },
        new() { ["OrderId"] = 1004, ["CustomerId"] = 55, ["OrderDate"] = "2026-03-22", ["TotalAmount"] = 1250.00m, ["Status"] = "Shipped" },
        new() { ["OrderId"] = 1005, ["CustomerId"] = 8, ["OrderDate"] = "2026-03-25", ["TotalAmount"] = 34.99m, ["Status"] = "Processing" },
    ];

    private static readonly List<Dictionary<string, object?>> _customers =
    [
        new() { ["CustomerId"] = 8, ["FirstName"] = "Alice", ["LastName"] = "Chen", ["Email"] = "alice@example.com", ["City"] = "Seattle", ["State"] = "WA" },
        new() { ["CustomerId"] = 17, ["FirstName"] = "Bob", ["LastName"] = "Martinez", ["Email"] = "bob@example.com", ["City"] = "Austin", ["State"] = "TX" },
        new() { ["CustomerId"] = 42, ["FirstName"] = "Carol", ["LastName"] = "Johnson", ["Email"] = "carol@example.com", ["City"] = "Denver", ["State"] = "CO" },
        new() { ["CustomerId"] = 55, ["FirstName"] = "David", ["LastName"] = "Kim", ["Email"] = "david@example.com", ["City"] = "Portland", ["State"] = "OR" },
    ];

    private static readonly List<Dictionary<string, object?>> _products =
    [
        new() { ["ProductId"] = 1, ["Name"] = "Wireless Keyboard", ["Category"] = "Electronics", ["Price"] = 49.99m, ["StockQuantity"] = 150 },
        new() { ["ProductId"] = 2, ["Name"] = "USB-C Hub", ["Category"] = "Electronics", ["Price"] = 34.99m, ["StockQuantity"] = 200 },
        new() { ["ProductId"] = 3, ["Name"] = "Standing Desk Mat", ["Category"] = "Office", ["Price"] = 89.00m, ["StockQuantity"] = 75 },
        new() { ["ProductId"] = 4, ["Name"] = "Monitor Light Bar", ["Category"] = "Electronics", ["Price"] = 59.99m, ["StockQuantity"] = 120 },
    ];

    public Task<SqlExecutionResult> ExecuteAsync(string sql)
    {
        var upperSql = sql.ToUpperInvariant();

        List<Dictionary<string, object?>> rows;
        if (upperSql.Contains("CUSTOMERS"))
            rows = _customers;
        else if (upperSql.Contains("PRODUCTS"))
            rows = _products;
        else if (upperSql.Contains("COUNT"))
            rows = [new() { ["OrderCount"] = _orders.Count }];
        else if (upperSql.Contains("AVG"))
            rows = [new() { ["AvgOrderValue"] = 364.70m }];
        else
            rows = _orders;

        return Task.FromResult(SqlExecutionResult.Ok(rows));
    }
}
