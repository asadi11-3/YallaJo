namespace YallaJo.Web.Areas.Provider.Models.ProviderDocuments;

// Mirrors Booking.Domain.Enums.DocumentType (sent/received as the underlying int).
public enum DocumentType
{
    License = 0,
    Insurance = 1,
    Certificate = 2,
    Identity = 3,
    Other = 4,
    IndependentGuideID = 10,
    GovernmentID = 11,
    MoTALicense = 12,
    TourismAuthorityLicense = 13,
    InsuranceCertificate = 14,
    BusinessLicense = 15,
    TaxRegistration = 16,
    FirstAidCertification = 17,
    HealthSafetyCertificate = 18,
    FireSafetyCertificate = 19,
    ActivityCertification = 20,
    LiabilityInsurance = 21,
    ProofOfOwnership = 22,
    AffiliatedGuideList = 23,
    AgencyRegistration = 24,
}

// Mirrors Booking.Domain.Enums.DocumentStatus.
public enum DocumentStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Expired = 3,
}

// GET /api/v1/booking/provider/documents[/{id}]
public sealed class ProviderDocumentResponse
{
    public Guid Id { get; init; }
    public Guid? TourGuideId { get; init; }
    public Guid? BusinessId { get; init; }
    public DocumentType Type { get; init; }
    public string? FileName { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DocumentStatus Status { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? RejectionReason { get; init; }
    public DateTime CreatedAt { get; init; }
    public string RowVersion { get; init; } = "";
}

// POST /api/v1/booking/provider/documents (201)
public sealed class UploadProviderDocumentResponse
{
    public Guid Id { get; init; }
}
