namespace YallaJo.Web.Areas.Admin.Models.Blogs;

public static class BlogsMapper
{
    // ── List rows ───────────────────────────────────────────────────────────────
    public static BlogRowVm ToRowVm(BlogSummaryResponse r) => new()
    {
        Id          = r.Id,
        Title       = r.Title,
        Slug        = r.Slug,
        StatusLabel = "Published",
        PublishedAt = r.PublishedAt,
        ViewCount   = r.ViewCount,
        IsFeatured  = r.IsFeatured,
        PlaceId     = r.PlaceId,
    };

    public static BlogRowVm ToRowVm(AdminDeletedBlogResponse r) => new()
    {
        Id          = r.Id,
        Title       = r.Title,
        Slug        = r.Slug,
        StatusLabel = string.IsNullOrWhiteSpace(r.Status) ? "Deleted" : r.Status,
        PublishedAt = r.PublishedAt,
        DeletedAt   = r.DeletedAt,
        IsFeatured  = r.IsFeatured,
        PlaceId     = r.PlaceId,
        RowVersion  = EncodeRowVersion(r.RowVersion),
    };

    // ── Edit form ────────────────────────────────────────────────────────────────
    public static EditBlogVm ToEditVm(AdminBlogDetailResponse r) =>
        ToEditVm(r, [], []);

    public static EditBlogVm ToEditVm(
        AdminBlogDetailResponse r,
        IReadOnlyList<LinkedTourVm> linkedTours,
        IReadOnlyList<TourOptionVm> availableTours) => new()
    {
        Id              = r.Id,
        RowVersion      = EncodeRowVersion(r.RowVersion),
        Title           = r.Title,
        Slug            = r.Slug,
        Content         = r.Content,
        Summary         = r.Summary,
        MetaTitle       = r.MetaTitle,
        MetaDescription = r.MetaDescription,
        PlaceId         = r.PlaceId,
        ReadTimeMinutes = r.ReadTimeMinutes,
        StatusLabel     = r.Status,
        IsFeatured      = r.IsFeatured,
        PublishedAt     = r.PublishedAt,
        ViewCount       = r.ViewCount,
        LanguageCode    = r.LanguageCode,
        LinkedTours     = linkedTours,
        AvailableTours  = availableTours,
    };

    // ── Requests ─────────────────────────────────────────────────────────────────
    public static CreateBlogRequest ToCreateRequest(CreateBlogVm vm) => new(
        Title:              vm.Title.Trim(),
        Content:            vm.Content.Trim(),
        SourceLanguageCode: vm.SourceLanguageCode.Trim(),
        Slug:               Normalize(vm.Slug),
        Summary:            Normalize(vm.Summary),
        MetaTitle:          Normalize(vm.MetaTitle),
        MetaDescription:    Normalize(vm.MetaDescription),
        PlaceId:            vm.PlaceId);

    public static UpdateBlogRequest ToUpdateRequest(EditBlogVm vm) => new(
        RowVersion:      DecodeRowVersion(vm.RowVersion),
        Title:           vm.Title.Trim(),
        Slug:            vm.Slug.Trim(),
        Content:         vm.Content.Trim(),
        Summary:         Normalize(vm.Summary),
        MetaTitle:       Normalize(vm.MetaTitle),
        MetaDescription: Normalize(vm.MetaDescription),
        PlaceId:         vm.PlaceId,
        ReadTimeMinutes: vm.ReadTimeMinutes);

    // ── RowVersion helpers ───────────────────────────────────────────────────────
    public static string EncodeRowVersion(byte[]? rowVersion)
        => rowVersion is { Length: > 0 } ? Convert.ToBase64String(rowVersion) : string.Empty;

    public static byte[] DecodeRowVersion(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64)) return [];
        try { return Convert.FromBase64String(base64); }
        catch (FormatException) { return []; }
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
