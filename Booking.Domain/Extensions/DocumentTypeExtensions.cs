using System.Collections.Generic;
using Booking.Domain.Enums;

namespace Booking.Domain.Extensions;

public static class DocumentTypeExtensions
{
    private static readonly HashSet<DocumentType> CriticalDocs = new()
    {
        DocumentType.MoTALicense,
        DocumentType.InsuranceCertificate,
        DocumentType.LiabilityInsurance,
        DocumentType.HealthSafetyCertificate,
        DocumentType.FireSafetyCertificate,
        DocumentType.TourismAuthorityLicense
    };

    public static bool IsCritical(DocumentType type) => CriticalDocs.Contains(type);

    public static int MaxFileSizeBytes(DocumentType type) => 10 * 1024 * 1024;
}
