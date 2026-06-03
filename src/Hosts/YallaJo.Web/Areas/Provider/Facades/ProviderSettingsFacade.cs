using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.Settings;

namespace YallaJo.Web.Areas.Provider.Facades;

public enum ProviderSettingsOutcome
{
    Ok,
    ForceSignOut,
    NoApplication,  // 404 — no provider application exists
    Error,
}

public sealed record ProviderSettingsResult(
    ProviderSettingsOutcome Outcome,
    ProviderSettingsVm? Settings = null,
    string? Error = null);

public sealed class ProviderSettingsFacade
{
    private readonly ProviderSettingsApiClient _api;

    public ProviderSettingsFacade(ProviderSettingsApiClient api) => _api = api;

    public async Task<ProviderSettingsResult> GetSettingsAsync(CancellationToken ct = default)
    {
        var result = await _api.GetSettingsAsync(ct);

        if (result.IsUnauthorized) return new(ProviderSettingsOutcome.ForceSignOut);
        if (result.IsNotFound) return new(ProviderSettingsOutcome.NoApplication);
        if (!result.IsSuccess || result.Data is null)
            return new(ProviderSettingsOutcome.Error,
                Error: result.Error ?? "Could not load your provider settings.");

        return new(ProviderSettingsOutcome.Ok, ProviderSettingsMapper.ToVm(result.Data));
    }
}
