namespace Sentinel.Api.Configuration;

public class SafeSchema
{
    public List<AllowedObject> AllowedObjects { get; set; } = [];
}

public class AllowedObject
{
    public string Schema { get; set; } = "dbo";
    public string Table { get; set; } = "";
    public List<string> Columns { get; set; } = [];
}
