namespace Analytics.Domain.Entities;

public sealed class DashboardCache
{
    private DashboardCache() { }
    public string Key { get; private set; } = string.Empty;
    public string ValueJson { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime RebuiltAt { get; private set; }
    public static DashboardCache Create(string key, string valueJson, DateTime expiresAt, DateTime rebuiltAt)
        => new() { Key = key, ValueJson = valueJson, ExpiresAt = expiresAt, RebuiltAt = rebuiltAt };
    public void Update(string valueJson, DateTime expiresAt, DateTime rebuiltAt)
    {
        ValueJson = valueJson;
        ExpiresAt = expiresAt;
        RebuiltAt = rebuiltAt;
    }
}
