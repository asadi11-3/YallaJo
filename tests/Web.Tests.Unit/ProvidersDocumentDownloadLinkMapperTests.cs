using FluentAssertions;
using YallaJo.Web.Areas.Admin.Models.Providers;

namespace Web.Tests.Unit;

/// <summary>
/// Patch 2F — verifies the Admin "Provider details" document row renders the
/// authorized MVC download-proxy link (<c>/admin/providers/documents/{id}/download</c>)
/// instead of the legacy on-disk <c>FileUrl</c> (which the API static-file middleware
/// 404s). Also pins that the legacy on-disk URL is never surfaced into the view model.
/// </summary>
public sealed class ProvidersDocumentDownloadLinkMapperTests
{
    private const string LegacyOnDiskUrl =
        "/uploads/provider-application-documents/11111111-1111-1111-1111-111111111111.pdf";

    [Fact]
    public void ToDetailsVm_DocumentRow_UsesAuthorizedDownloadProxyLink()
    {
        var documentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var response = BuildResponseWithDocument(documentId);

        var vm = ProvidersMapper.ToDetailsVm(response);

        vm.Documents.Should().ContainSingle();
        var doc = vm.Documents[0];
        doc.DownloadUrl.Should().Be($"/admin/providers/documents/{documentId}/download");
    }

    [Fact]
    public void ToDetailsVm_DocumentRow_DoesNotLeakLegacyOnDiskUrl()
    {
        var documentId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var response = BuildResponseWithDocument(documentId);

        var vm = ProvidersMapper.ToDetailsVm(response);

        var doc = vm.Documents[0];
        // The legacy on-disk URL must never reach the view model in any field.
        doc.DownloadUrl.Should().NotContain("/uploads/");
        doc.DownloadUrl.Should().NotBe(LegacyOnDiskUrl);
        doc.FileName.Should().Be("national-id.pdf");
        doc.DownloadUrl.Should().StartWith("/admin/providers/documents/");
        doc.DownloadUrl.Should().EndWith("/download");
    }

    private static AdminProviderApplicationDetailsResponse BuildResponseWithDocument(Guid documentId) => new()
    {
        ApplicationId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        ContactEmail = "owner@example.com",
        Type = "TourOperator",
        BusinessName = "Acme Tours",
        ContactPhone = "+97400000000",
        Address = "Doha",
        Description = "Tours",
        Status = "Submitted",
        Documents =
        [
            new AdminProviderDocumentResponse
            {
                DocumentId = documentId,
                DocumentType = "GovernmentId",
                FileName = "national-id.pdf",
                FileSizeBytes = 4242,
                UploadedAt = new DateTime(2026, 4, 26, 12, 0, 0, DateTimeKind.Utc),
                ExpiresAt = null,
            },
        ],
    };
}
