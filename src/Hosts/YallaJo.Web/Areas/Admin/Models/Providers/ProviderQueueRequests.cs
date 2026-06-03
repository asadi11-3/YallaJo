namespace YallaJo.Web.Areas.Admin.Models.Providers;

/// <summary>Body for <c>POST /api/v1/admin/providers/{id}/reject</c>.</summary>
public sealed record RejectProviderRequest(string Reason);

/// <summary>
/// Body for <c>POST /api/v1/admin/providers/{id}/request-docs</c>.
/// <see cref="MissingDocumentTypes"/> carries enum names; the API accepts string enum values.
/// </summary>
public sealed record RequestMoreDocsRequest(
    IReadOnlyList<string> MissingDocumentTypes,
    string Notes);

/// <summary>Body for <c>POST /api/v1/admin/providers/{id}/suspend</c>.</summary>
public sealed record SuspendProviderRequest(string Reason);
