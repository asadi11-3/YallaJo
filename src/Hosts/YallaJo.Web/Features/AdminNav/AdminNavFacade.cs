namespace YallaJo.Web.Features.AdminNav;

/// <summary>
/// Composes the admin-nav view model from the DB-backed security snapshot.
/// Never throws / never blocks the shell: any failure degrades to a non-DB-backed
/// VM so the layout falls back to JWT-claim gating (ERR3).
/// Auto-registered (scoped-self) by AddFeatureServices() via the 'Facade' suffix.
/// </summary>
public sealed class AdminNavFacade
{
    private readonly AdminNavApiClient _api;
    private readonly ILogger<AdminNavFacade> _logger;

    public AdminNavFacade(AdminNavApiClient api, ILogger<AdminNavFacade> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<AdminNavVm> GetNavAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _api.GetMeAsync(ct);
            if (result.IsSuccess && result.Data is not null)
            {
                return new AdminNavVm
                {
                    Roles = result.Data.Roles
                        .ToHashSet(StringComparer.OrdinalIgnoreCase),
                    Permissions = result.Data.Permissions
                        .ToHashSet(StringComparer.Ordinal),
                    IsDbBacked = true,
                };
            }

            return NotDbBacked();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load DB-backed security snapshot for admin nav; falling back to JWT claims");
            return NotDbBacked();
        }
    }

    private static AdminNavVm NotDbBacked() => new() { Permissions = [], IsDbBacked = false };
}
