namespace YallaJo.Web.Areas.Provider.Models.Settings;

public sealed class ProviderSettingsResponse
{
    public string BusinessName { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ProviderType { get; init; } = string.Empty;
}
