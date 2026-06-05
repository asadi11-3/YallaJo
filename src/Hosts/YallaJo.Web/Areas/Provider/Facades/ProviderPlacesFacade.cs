using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.Tours;

namespace YallaJo.Web.Areas.Provider.Facades;

public sealed record PlaceOptionsResult(
    IReadOnlyList<PlaceOptionVm> Options,
    bool LoadFailed,
    bool ForceSignOut,
    string? Error = null)
{
    public static PlaceOptionsResult Success(IReadOnlyList<PlaceOptionVm> options) =>
        new(options, LoadFailed: false, ForceSignOut: false);

    public static PlaceOptionsResult SignOut() =>
        new([], LoadFailed: true, ForceSignOut: true);

    public static PlaceOptionsResult Failed(string? error) =>
        new([], LoadFailed: true, ForceSignOut: false, error);
}

public sealed class ProviderPlacesFacade
{
    private const string DefaultLoadError =
        "Places could not be loaded right now. You can try again after reloading the page.";

    private readonly ProviderPlacesApiClient _api;

    public ProviderPlacesFacade(ProviderPlacesApiClient api) => _api = api;

    /// <summary>
    /// Loads the selectable place options for the tour form. Sorted by name for
    /// a stable, scannable dropdown.
    /// </summary>
    public async Task<PlaceOptionsResult> GetPlaceOptionsAsync(CancellationToken ct = default)
    {
        var result = await _api.ListAsync(ct);

        if (result.IsUnauthorized) return PlaceOptionsResult.SignOut();
        if (!result.IsSuccess || result.Data is null)
            return PlaceOptionsResult.Failed(result.Error ?? DefaultLoadError);

        var options = result.Data.Items
            .Select(p => new PlaceOptionVm
            {
                Id         = p.Id,
                Name       = p.Name,
                City       = p.City,
                Country    = p.Country,
                IsVerified = p.IsVerified,
            })
            .OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return PlaceOptionsResult.Success(options);
    }
}
