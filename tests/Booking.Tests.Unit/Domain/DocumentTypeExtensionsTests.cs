using Booking.Domain.Enums;
using Booking.Domain.Extensions;
using FluentAssertions;

namespace Booking.Tests.Unit.Domain;

public sealed class DocumentTypeExtensionsTests
{
    public static IEnumerable<object[]> CriticalCases =>
        new List<object[]>
        {
            new object[] { DocumentType.MoTALicense },
            new object[] { DocumentType.InsuranceCertificate },
            new object[] { DocumentType.LiabilityInsurance },
            new object[] { DocumentType.HealthSafetyCertificate },
            new object[] { DocumentType.FireSafetyCertificate },
            new object[] { DocumentType.TourismAuthorityLicense },
        };

    public static IEnumerable<object[]> NonCriticalCases =>
        new List<object[]>
        {
            new object[] { DocumentType.License },
            new object[] { DocumentType.Insurance },
            new object[] { DocumentType.Certificate },
            new object[] { DocumentType.Identity },
            new object[] { DocumentType.Other },
            new object[] { DocumentType.IndependentGuideID },
            new object[] { DocumentType.GovernmentID },
            new object[] { DocumentType.BusinessLicense },
            new object[] { DocumentType.TaxRegistration },
            new object[] { DocumentType.FirstAidCertification },
            new object[] { DocumentType.ActivityCertification },
            new object[] { DocumentType.ProofOfOwnership },
            new object[] { DocumentType.AffiliatedGuideList },
            new object[] { DocumentType.AgencyRegistration },
        };

    public static IEnumerable<object[]> RequiresExpiryCases =>
        new List<object[]>
        {
            new object[] { DocumentType.MoTALicense },
            new object[] { DocumentType.BusinessLicense },
            new object[] { DocumentType.TaxRegistration },
            new object[] { DocumentType.InsuranceCertificate },
            new object[] { DocumentType.LiabilityInsurance },
            new object[] { DocumentType.HealthSafetyCertificate },
            new object[] { DocumentType.FireSafetyCertificate },
            new object[] { DocumentType.ActivityCertification },
            new object[] { DocumentType.TourismAuthorityLicense },
            new object[] { DocumentType.FirstAidCertification },
        };

    [Theory]
    [MemberData(nameof(CriticalCases))]
    public void IsCritical_returns_true_for_critical_doc_types(DocumentType type)
    {
        type.IsCritical().Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(NonCriticalCases))]
    public void IsCritical_returns_false_for_non_critical_doc_types(DocumentType type)
    {
        type.IsCritical().Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(RequiresExpiryCases))]
    public void RequiresExpiry_returns_true_for_required_types(DocumentType type)
    {
        type.RequiresExpiry().Should().BeTrue();
    }

    [Theory]
    [InlineData(DocumentType.License)]
    [InlineData(DocumentType.Insurance)]
    [InlineData(DocumentType.Certificate)]
    [InlineData(DocumentType.Identity)]
    [InlineData(DocumentType.Other)]
    [InlineData(DocumentType.IndependentGuideID)]
    [InlineData(DocumentType.GovernmentID)]
    [InlineData(DocumentType.ProofOfOwnership)]
    [InlineData(DocumentType.AffiliatedGuideList)]
    [InlineData(DocumentType.AgencyRegistration)]
    public void RequiresExpiry_returns_false_for_non_required_types(DocumentType type)
    {
        type.RequiresExpiry().Should().BeFalse();
    }

    [Fact]
    public void MaxFileSizeBytes_returns_ten_megabytes_for_every_enum_value()
    {
        const int expected = 10 * 1024 * 1024;
        foreach (DocumentType type in Enum.GetValues(typeof(DocumentType)))
        {
            type.MaxFileSizeBytes().Should().Be(expected, $"failing for {type}");
        }

        DocumentTypeExtensions.MaxUploadBytes.Should().Be(expected);
    }
}
