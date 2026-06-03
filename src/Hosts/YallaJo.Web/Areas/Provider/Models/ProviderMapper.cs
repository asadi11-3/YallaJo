namespace YallaJo.Web.Areas.Provider.Models;

/// <summary>Static DTO ↔ ViewModel ↔ Request mapping for the provider self-service screens.</summary>
public static class ProviderMapper
{
    // Provider type enum names (must match Accounts.Domain.Enums.ProviderType) + display labels.
    private static readonly (string Value, string Label)[] ProviderTypes =
    {
        ("TourOperator",     "Tour operator"),
        ("IndependentGuide", "Independent guide"),
        ("HotelResort",      "Hotel / resort"),
        ("ActivityCenter",   "Activity center"),
        ("Agency",           "Agency"),
        ("BusinessOwner",    "Business owner"),
    };

    public static IReadOnlyList<ProviderTypeOptionVm> TypeOptions() =>
        ProviderTypes.Select(t => new ProviderTypeOptionVm { Value = t.Value, Label = t.Label }).ToList();

    public static RegisterProviderRequest ToRegisterRequest(ProviderApplyVm vm) => new(
        Type:         vm.Type.Trim(),
        BusinessName: vm.BusinessName.Trim(),
        ContactEmail: vm.ContactEmail.Trim(),
        ContactPhone: vm.ContactPhone.Trim(),
        Address:      vm.Address.Trim(),
        Description:  vm.Description.Trim());

    public static ProviderStatusVm ToStatusVm(ProviderStatusResponse r) => new()
    {
        HasApplication      = true,
        ApplicationId       = r.ApplicationId,
        TypeLabel           = Humanize(r.Type),
        BusinessName        = r.BusinessName,
        Status              = r.Status,
        SubmittedAt         = r.SubmittedAt,
        ReviewedAt          = r.ReviewedAt,
        RejectionReason     = r.RejectionReason,
        SuspensionReason    = r.SuspensionReason,
        ReapplicationCount  = r.ReapplicationCount,
        CoolingPeriodEndsAt = r.CoolingPeriodEndsAt,
        Documents           = r.Documents.Select(ToDocumentVm).ToList(),
    };

    public static ProviderStatusVm EmptyStatus() => new() { HasApplication = false };

    private static ProviderDocumentVm ToDocumentVm(ProviderDocumentResponse d) => new()
    {
        DocumentTypeLabel = Humanize(d.DocumentType),
        FileName          = d.FileName,
        ExpiresAt         = d.ExpiresAt,
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
