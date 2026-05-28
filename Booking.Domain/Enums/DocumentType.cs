namespace Booking.Domain.Enums;

public enum DocumentType : byte
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
