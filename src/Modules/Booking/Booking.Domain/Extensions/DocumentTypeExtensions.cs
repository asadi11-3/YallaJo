using Booking.Domain.Enums;

namespace Booking.Domain.Extensions;

public static class DocumentTypeExtensions
{
    public const int MaxUploadBytes = 10 * 1024 * 1024;

    public static readonly IReadOnlyList<DocumentType> AllCritical =
    [
        DocumentType.MoTALicense,
        DocumentType.InsuranceCertificate,
        DocumentType.LiabilityInsurance,
        DocumentType.HealthSafetyCertificate,
        DocumentType.FireSafetyCertificate,
        DocumentType.TourismAuthorityLicense,
    ];

    public static readonly IReadOnlyList<DocumentType> AllRequireExpiry =
    [
        DocumentType.MoTALicense,
        DocumentType.BusinessLicense,
        DocumentType.TaxRegistration,
        DocumentType.InsuranceCertificate,
        DocumentType.LiabilityInsurance,
        DocumentType.HealthSafetyCertificate,
        DocumentType.FireSafetyCertificate,
        DocumentType.ActivityCertification,
        DocumentType.TourismAuthorityLicense,
        DocumentType.FirstAidCertification,
    ];

    public static bool IsCritical(this DocumentType type)
        => AllCritical.Contains(type);

    public static bool RequiresExpiry(this DocumentType type)
        => AllRequireExpiry.Contains(type);

    public static int MaxFileSizeBytes(this DocumentType type)
        => MaxUploadBytes;
}
