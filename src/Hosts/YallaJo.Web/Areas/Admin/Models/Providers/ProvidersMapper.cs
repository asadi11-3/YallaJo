namespace YallaJo.Web.Areas.Admin.Models.Providers;

/// <summary>Static DTO ↔ ViewModel mapping for the admin provider-queue screens.</summary>
public static class ProvidersMapper
{
    // ── Enum option catalogs (names must match the Accounts.Domain enums) ───────────

    // ProviderApplicationStatus values usable as a queue filter.
    private static readonly (string Value, string Label)[] Statuses =
    {
        ("Draft",          "Draft"),
        ("Pending",        "Pending review"),
        ("MoreDocsNeeded", "More docs needed"),
        ("Approved",       "Approved"),
        ("Rejected",       "Rejected"),
        ("Suspended",      "Suspended"),
    };

    private static readonly (string Value, string Label)[] Types =
    {
        ("TourOperator",     "Tour operator"),
        ("IndependentGuide", "Independent guide"),
        ("HotelResort",      "Hotel / resort"),
        ("ActivityCenter",   "Activity center"),
        ("Agency",           "Agency"),
        ("BusinessOwner",    "Business owner"),
    };

    // DocumentType values (names must match Accounts.Domain.Enums.DocumentType).
    private static readonly (string Value, string Label)[] DocumentTypes =
    {
        ("BusinessLicense",         "Business license"),
        ("TaxRegistration",         "Tax registration"),
        ("TourismAuthorityLicense", "Tourism authority license"),
        ("InsuranceCertificate",    "Insurance certificate"),
        ("GovernmentId",            "Government ID"),
        ("MotaLicense",             "MOTA license"),
        ("TaxIdentificationNumber", "Tax identification number"),
        ("ProofOfOwnership",        "Proof of ownership"),
        ("HealthAndSafety",         "Health and safety"),
        ("FireSafety",              "Fire safety"),
        ("RelevantCertification",   "Relevant certification"),
        ("LiabilityInsurance",      "Liability insurance"),
        ("AffiliatedGuidesList",    "Affiliated guides list"),
    };

    public static IReadOnlyList<ProviderFilterOptionVm> StatusOptions() => ToOptions(Statuses);
    public static IReadOnlyList<ProviderFilterOptionVm> TypeOptions() => ToOptions(Types);
    public static IReadOnlyList<ProviderFilterOptionVm> DocumentTypeOptions() => ToOptions(DocumentTypes);

    private static IReadOnlyList<ProviderFilterOptionVm> ToOptions((string Value, string Label)[] src) =>
        src.Select(o => new ProviderFilterOptionVm { Value = o.Value, Label = o.Label }).ToList();

    // ── Queue mapping ──────────────────────────────────────────────────────────────

    public static ProviderQueueVm ToQueueVm(
        ProviderQueueResponse r, string? status, string? type) => new()
    {
        Items               = r.Items.Select(ToRowVm).ToList(),
        Status              = status,
        Type                = type,
        Page                = r.Page,
        PageSize            = r.PageSize,
        TotalCount          = r.TotalCount,
        StatusOptions       = StatusOptions(),
        TypeOptions         = TypeOptions(),
        DocumentTypeOptions = DocumentTypeOptions(),
    };

    /// <summary>Empty VM used when the queue could not be loaded (still renders filters).</summary>
    public static ProviderQueueVm EmptyQueue(string? status, string? type, int page, int pageSize) => new()
    {
        Status              = status,
        Type                = type,
        Page                = page,
        PageSize            = pageSize,
        StatusOptions       = StatusOptions(),
        TypeOptions         = TypeOptions(),
        DocumentTypeOptions = DocumentTypeOptions(),
    };

    private static ProviderQueueRowVm ToRowVm(ProviderQueueItemResponse i) => new()
    {
        ApplicationId      = i.ApplicationId,
        TypeLabel          = Humanize(i.Type),
        BusinessName       = i.BusinessName,
        ContactEmail       = i.ContactEmail,
        Status             = i.Status,
        StatusLabel        = Humanize(i.Status),
        StatusBadgeClass   = BadgeClass(i.Status),
        SubmittedAt        = i.SubmittedAt,
        ReviewedAt         = i.ReviewedAt,
        DocumentCount      = i.DocumentCount,
        ReapplicationCount = i.ReapplicationCount,
        // Action availability mirrors the backend state machine:
        //   Pending / MoreDocsNeeded → Approve, Reject, Request docs
        //   Approved                 → Suspend
        //   Suspended                → Reinstate
        CanApprove     = IsReviewable(i.Status),
        CanReject      = IsReviewable(i.Status),
        CanRequestDocs = IsReviewable(i.Status),
        CanSuspend     = Eq(i.Status, "Approved"),
        CanReinstate   = Eq(i.Status, "Suspended"),
    };

    private static bool IsReviewable(string status) =>
        Eq(status, "Pending") || Eq(status, "MoreDocsNeeded");

    private static bool Eq(string a, string b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static string BadgeClass(string status) => status switch
    {
        _ when Eq(status, "Approved")       => "text-bg-success",
        _ when Eq(status, "Pending")        => "text-bg-warning",
        _ when Eq(status, "MoreDocsNeeded") => "text-bg-info",
        _ when Eq(status, "Rejected")       => "text-bg-danger",
        _ when Eq(status, "Suspended")      => "text-bg-dark",
        _                                   => "text-bg-secondary",
    };

    // "TourOperator" → "Tour operator", "MoreDocsNeeded" → "More docs needed".
    public static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var sb = new System.Text.StringBuilder(value.Length + 4);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(value[i - 1]))
                sb.Append(' ');
            sb.Append(i == 0 ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
