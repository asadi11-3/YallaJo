namespace YallaJo.Web.Areas.Accounts.Models.Privacy;

/// <summary>
/// FE-1D — Accounts-local mirror of the Analytics <c>UserDataExportDto</c>
/// (GET /api/v1/analytics/recommendations/me/export).
/// <para>
/// NOTE: the export download itself is proxied as raw bytes through
/// <see cref="ApiClients.PrivacyApiClient.ExportAsync"/> (no typed round-trip), so this
/// model is intentionally only a documentation/reference shape of what the export
/// contains. It is not used to re-serialize the download.
/// </para>
/// </summary>
public sealed class UserDataExportResponse
{
    public Guid UserId { get; set; }
    public IReadOnlyList<object> Interactions { get; set; } = [];
    public object? Preferences { get; set; }
    public IReadOnlyList<object> ExcludedEntities { get; set; } = [];
    public DateTime ExportedAt { get; set; }
}
