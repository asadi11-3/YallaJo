namespace YallaJo.Web.Areas.Creator.Models.Articles;

/// <summary>
/// Outbound body for the RowVersion-guarded blog mutations (submit-for-review,
/// delete, restore). The backend <c>BlogRowVersionRequest</c> binds
/// <c>byte[] RowVersion</c>; over JSON that round-trips as a base64 string, so we
/// carry the verbatim base64 value obtained from the admin-get prefetch.
/// </summary>
public sealed record BlogRowVersionRequestBody(string RowVersion);

/// <summary>
/// Outbound body for <c>POST /api/v1/blogs/{id}/tours</c> (link related tours).
/// Mirrors the backend <c>BlogLinkToursRequest(RowVersion, Tours[])</c>; carries the
/// verbatim base64 RowVersion from the admin-get prefetch (ST1 optimistic concurrency).
/// </summary>
public sealed record BlogLinkToursRequestBody(string RowVersion, IReadOnlyCollection<BlogLinkTourItemBody> Tours);

/// <summary>One tour to link, mirroring the backend <c>BlogLinkTourItem(TourId, SortOrder?)</c>.</summary>
public sealed record BlogLinkTourItemBody(Guid TourId, int? SortOrder = null);
