namespace YallaJo.Web.Areas.Creator.Models.Articles;

/// <summary>
/// Outbound body for the RowVersion-guarded blog mutations (submit-for-review,
/// delete, restore). The backend <c>BlogRowVersionRequest</c> binds
/// <c>byte[] RowVersion</c>; over JSON that round-trips as a base64 string, so we
/// carry the verbatim base64 value obtained from the admin-get prefetch.
/// </summary>
public sealed record BlogRowVersionRequestBody(string RowVersion);
