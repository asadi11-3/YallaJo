using System.Globalization;

namespace YallaJo.Web.Areas.Provider.Models.Tours;

/// <summary>Static DTO ↔ ViewModel ↔ request mapping for provider tour/listing screens.</summary>
public static class ProviderToursMapper
{
    private static readonly (string Value, string Label)[] Statuses =
    {
        ("Draft", "Draft"),
        ("Pending", "Pending review"),
        ("Approved", "Approved"),
        ("Rejected", "Rejected"),
        ("Suspended", "Suspended"),
        ("Archived", "Archived"),
    };

    public static IReadOnlyList<ProviderTourStatusOption> StatusOptions() =>
        Statuses.Select(s => new ProviderTourStatusOption { Value = s.Value, Label = s.Label }).ToList();

    // ── List ────────────────────────────────────────────────────────────────────────

    public static ProviderToursIndexVm ToIndexVm(ListMyToursResponse r, string? status) => new()
    {
        Items         = r.Items.Select(ToRowVm).ToList(),
        Status        = status,
        Page          = r.Page,
        PageSize      = r.PageSize,
        Total         = r.Total,
        TotalPages    = r.TotalPages,
        StatusOptions = StatusOptions(),
    };

    public static ProviderToursIndexVm EmptyIndex(string? status, int page, int pageSize) => new()
    {
        Status        = status,
        Page          = page,
        PageSize      = pageSize,
        StatusOptions = StatusOptions(),
    };

    private static ProviderTourRowVm ToRowVm(TourSummaryResponse t) => new()
    {
        Id               = t.Id,
        Name             = t.Name,
        Status           = t.Status,
        StatusLabel      = Humanize(t.Status),
        StatusBadgeClass = BadgeClass(t.Status),
        BasePrice        = t.BasePrice,
        Currency         = t.Currency,
        CreatedAt        = t.CreatedAt,
        CanEdit          = CanEdit(t.Status),
        CanSubmit        = CanSubmit(t.Status),
        CanArchive       = CanArchive(t.Status),
    };

    // ── Form (edit prefill) ──────────────────────────────────────────────────────────

    public static ProviderTourFormVm ToFormVm(TourDetailResponse d) => new()
    {
        TourId                = d.Id,
        RowVersion            = d.RowVersion is { Length: > 0 } rv ? Convert.ToBase64String(rv) : null,
        Name                  = d.Name,
        Slug                  = d.Slug,
        Difficulty            = string.IsNullOrWhiteSpace(d.Difficulty) ? "Easy" : d.Difficulty,
        DurationMinutes       = d.DurationMinutes,
        MaxGroupSize          = d.MaxGroupSize,
        BasePrice             = d.BasePrice,
        Currency              = d.Currency,
        Latitude              = d.Latitude,
        Longitude             = d.Longitude,
        ShortDescription      = d.ShortDescription,
        Description           = d.Description,
        PlaceId               = d.PlaceId,
        IsChildFriendly       = d.IsChildFriendly,
        IsAccessible          = d.IsAccessible,
        CancellationPolicyHours = d.CancellationPolicyHours,
        MeetingPointLatitude  = d.MeetingPointLatitude,
        MeetingPointLongitude = d.MeetingPointLongitude,
        Status                = d.Status,
        StatusLabel           = Humanize(d.Status),
    };

    // ── Form → API request ───────────────────────────────────────────────────────────

    public static CreateTourApiRequest ToCreateRequest(ProviderTourFormVm vm) => new(
        Name:                  vm.Name.Trim(),
        Slug:                  vm.Slug.Trim().ToLowerInvariant(),
        Difficulty:            vm.Difficulty,
        DurationMinutes:       vm.DurationMinutes,
        MaxGroupSize:          vm.MaxGroupSize,
        BasePrice:             vm.BasePrice,
        Currency:              vm.Currency.Trim().ToUpperInvariant(),
        Latitude:              vm.Latitude,
        Longitude:             vm.Longitude,
        Description:           Trimmed(vm.Description),
        ShortDescription:      Trimmed(vm.ShortDescription),
        PlaceId:               vm.PlaceId,
        IsChildFriendly:       vm.IsChildFriendly,
        IsAccessible:          vm.IsAccessible,
        CancellationPolicyHours: vm.CancellationPolicyHours,
        MeetingPointLatitude:  vm.MeetingPointLatitude,
        MeetingPointLongitude: vm.MeetingPointLongitude);

    public static UpdateTourApiRequest ToUpdateRequest(ProviderTourFormVm vm, byte[] rowVersion) => new(
        RowVersion:            rowVersion,
        Name:                  vm.Name.Trim(),
        Slug:                  vm.Slug.Trim().ToLowerInvariant(),
        Difficulty:            vm.Difficulty,
        DurationMinutes:       vm.DurationMinutes,
        MaxGroupSize:          vm.MaxGroupSize,
        BasePrice:             vm.BasePrice,
        Currency:              vm.Currency.Trim().ToUpperInvariant(),
        Latitude:              vm.Latitude,
        Longitude:             vm.Longitude,
        Description:           Trimmed(vm.Description),
        ShortDescription:      Trimmed(vm.ShortDescription),
        PlaceId:               vm.PlaceId,
        IsChildFriendly:       vm.IsChildFriendly,
        IsAccessible:          vm.IsAccessible,
        CancellationPolicyHours: vm.CancellationPolicyHours,
        MeetingPointLatitude:  vm.MeetingPointLatitude,
        MeetingPointLongitude: vm.MeetingPointLongitude);

    // ── Status helpers ───────────────────────────────────────────────────────────────

    private static bool Eq(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    // Editable: Draft or Rejected (backend PUT allows draft/rejected).
    public static bool CanEdit(string status) => Eq(status, "Draft") || Eq(status, "Rejected");
    // Submit: Draft only (backend SubmitTour requires Draft).
    public static bool CanSubmit(string status) => Eq(status, "Draft");
    // Archive: provider may archive non-terminal own tours (Draft/Rejected/Approved/Suspended).
    public static bool CanArchive(string status) =>
        Eq(status, "Draft") || Eq(status, "Rejected") || Eq(status, "Approved") || Eq(status, "Suspended");

    private static string BadgeClass(string status) => status switch
    {
        _ when Eq(status, "Approved")  => "text-bg-success",
        _ when Eq(status, "Pending")   => "text-bg-warning",
        _ when Eq(status, "Rejected")  => "text-bg-danger",
        _ when Eq(status, "Suspended") => "text-bg-dark",
        _ when Eq(status, "Archived")  => "text-bg-secondary",
        _                              => "text-bg-light",
    };

    private static string? Trimmed(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    // "MoreDocsNeeded" → "More docs needed"; single words pass through capitalized.
    public static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var sb = new System.Text.StringBuilder(value.Length + 4);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(value[i - 1])) sb.Append(' ');
            sb.Append(i == 0 ? char.ToUpper(c, CultureInfo.InvariantCulture) : char.ToLower(c, CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }
}
