namespace Sentinel.Api.Configuration;

public class SentinelOptions
{
    public int QueryTimeoutSeconds { get; set; } = 30;
    public int MaxRowCount { get; set; } = 1000;
    public int MaxCorrectionRetries { get; set; } = 2;
}
