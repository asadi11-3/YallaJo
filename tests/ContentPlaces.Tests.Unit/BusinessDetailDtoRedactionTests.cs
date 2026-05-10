using ContentPlaces.Application.Queries.Business.Common;
using FluentAssertions;

namespace ContentPlaces.Tests.Unit;

/// <summary>
/// CONTENTPLACES-FOLLOWUP-DTO-REDACTION-001 regression tests.
///
/// <para>
/// <c>GET /places/businesses/{id}</c> is an <c>AllowAnonymous</c> public read
/// endpoint that returns <see cref="BusinessDetailDto"/>.  Following the same
/// clean-public-DTO principle established by ContentTours P1-007, the public
/// DTO must NOT carry owner identity or business credential fields:
/// </para>
///
/// <list type="bullet">
///   <item><c>OwnerId</c> — owner user identity (PII)</item>
///   <item><c>LicenseNumber</c> — business credential</item>
///   <item><c>TaxId</c> — business credential</item>
/// </list>
///
/// <para>
/// These reflection-only tests pin the DTO shape so admin/internal fields cannot
/// silently leak back onto the public response.  If admin/management screens
/// later need any of these fields, a separate <b>protected</b> management query
/// and DTO must be introduced.
/// </para>
/// </summary>
public sealed class BusinessDetailDtoRedactionTests
{
    [Fact]
    public void BusinessDetailDto_DoesNotDeclareOwnerId()
    {
        typeof(BusinessDetailDto)
            .GetProperty("OwnerId")
            .Should().BeNull(
                "OwnerId must be removed from the public BusinessDetailDto. " +
                "GET /places/businesses/{id} is AllowAnonymous; re-introducing this " +
                "field would leak owner identity to anonymous callers " +
                "(CONTENTPLACES-FOLLOWUP-DTO-REDACTION-001).");
    }

    [Fact]
    public void BusinessDetailDto_DoesNotDeclareLicenseNumber()
    {
        typeof(BusinessDetailDto)
            .GetProperty("LicenseNumber")
            .Should().BeNull(
                "LicenseNumber is a business credential and must not appear on the " +
                "public BusinessDetailDto.  Admin/management UI must use a separate " +
                "protected management query/DTO.");
    }

    [Fact]
    public void BusinessDetailDto_DoesNotDeclareTaxId()
    {
        typeof(BusinessDetailDto)
            .GetProperty("TaxId")
            .Should().BeNull(
                "TaxId is a business credential and must not appear on the public " +
                "BusinessDetailDto.  Admin/management UI must use a separate " +
                "protected management query/DTO.");
    }

    [Fact]
    public void GetBusinessById_PublicProjection_DoesNotExposeOwnerIdLicenseNumberTaxId()
    {
        // The projection in GetBusinessByIdQueryHandler builds a BusinessDetailDto
        // by directly invoking its constructor.  Because the DTO no longer declares
        // OwnerId / LicenseNumber / TaxId, the projection cannot leak them — the
        // type system guarantees absence at compile time.  This test fails if any
        // of those properties is reintroduced on the DTO record.
        var declared = typeof(BusinessDetailDto)
            .GetProperties()
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);

        declared.Should().NotContain("OwnerId");
        declared.Should().NotContain("LicenseNumber");
        declared.Should().NotContain("TaxId");
    }
}
