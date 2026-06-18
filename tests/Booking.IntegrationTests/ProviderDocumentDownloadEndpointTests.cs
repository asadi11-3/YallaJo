using Booking.Application.Queries.DownloadProviderDocument;
using Booking.Contracts.Authorization;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.IntegrationTests;

/// <summary>
/// Patch B2 — verifies the authorized Booking provider-document download path
/// (<c>GET /api/v1/booking/provider/documents/{id}/download</c>) exercised through its
/// MediatR query handler.
///
/// These tests prove the security guarantee that prompted the patch: a private provider
/// document (licenses, insurance certificates — PII) is streamed ONLY to its owning tour
/// guide or an admin, and NEVER to a cross-provider caller or an anonymous request. The
/// bytes are resolved by an ownership-checked handler, not by the public static-file
/// middleware (which Patch B1 now 404s for <c>/uploads/provider-documents</c>).
///
/// The handler is exercised directly with stubbed <see cref="IProviderDocumentRepository"/>,
/// <see cref="IFileStorageService"/> and <see cref="ICurrentUser"/> so no host or database
/// is required — the authorization branching is the contract under test.
/// </summary>
public sealed class ProviderDocumentDownloadEndpointTests
{
    private static readonly byte[] KnownBytes = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37]; // "%PDF-1.7"
    private const string KnownContentType = "application/pdf";
    private const string KnownFileName = "mota-license.pdf";

    private static readonly Guid OwnerUserId = Guid.Parse("0a000000-0000-0000-0000-000000000001");
    private static readonly Guid OtherUserId = Guid.Parse("0b000000-0000-0000-0000-000000000002");
    private static readonly Guid AdminUserId = Guid.Parse("0c000000-0000-0000-0000-000000000003");
    private static readonly Guid OwnerTourGuideId = Guid.Parse("0d000000-0000-0000-0000-0000000000a1");
    private static readonly Guid DocumentId = Guid.Parse("0e000000-0000-0000-0000-0000000000f1");
    private const string SecretDocumentUrl = "/uploads/provider-documents/SECRET-blob.pdf";

    [Fact]
    public async Task Owner_can_download_their_own_document_and_receives_the_bytes()
    {
        var document = BuildDocument();
        var repo = Substitute.For<IProviderDocumentRepository>();
        repo.GetTourGuideIdByUserIdAsync(OwnerUserId, Arg.Any<CancellationToken>())
            .Returns(OwnerTourGuideId);
        repo.GetByIdForTourGuideAsync(DocumentId, OwnerTourGuideId, Arg.Any<CancellationToken>())
            .Returns(document);

        var handler = BuildHandler(repo, OwnerUserId, isAdmin: false, fileBytes: KnownBytes);

        var result = await handler.Handle(new DownloadProviderDocumentQuery(DocumentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ContentType.Should().Be(KnownContentType);
        (await ReadAllAsync(result.Value.Content)).Should().Equal(KnownBytes);
        // The owner's document is resolved via the tour-guide path, never via GetByIdAsync (admin path).
        await repo.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Admin_can_download_any_providers_document()
    {
        var document = BuildDocument();
        var repo = Substitute.For<IProviderDocumentRepository>();
        repo.GetByIdAsync(DocumentId, Arg.Any<CancellationToken>())
            .Returns(document);

        var handler = BuildHandler(repo, AdminUserId, isAdmin: true, fileBytes: KnownBytes);

        var result = await handler.Handle(new DownloadProviderDocumentQuery(DocumentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await ReadAllAsync(result.Value!.Content)).Should().Equal(KnownBytes);
        // Admin resolves by id directly; the tour-guide ownership lookup is never used.
        await repo.DidNotReceive().GetTourGuideIdByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cross_provider_caller_gets_NotFound_and_no_bytes()
    {
        var repo = Substitute.For<IProviderDocumentRepository>();
        // The caller IS a tour guide, but the document does not belong to them, so the
        // ownership-scoped lookup returns null. The handler returns NotFound (not Forbidden)
        // so a foreign document's existence cannot be enumerated.
        repo.GetTourGuideIdByUserIdAsync(OtherUserId, Arg.Any<CancellationToken>())
            .Returns(Guid.Parse("0d000000-0000-0000-0000-0000000000b2"));
        repo.GetByIdForTourGuideAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ProviderDocument?)null);

        var fileStorage = Substitute.For<IFileStorageService>();
        var handler = BuildHandler(repo, OtherUserId, isAdmin: false, fileStorage: fileStorage);

        var result = await handler.Handle(new DownloadProviderDocumentQuery(DocumentId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
        // No bytes were ever read because no document was resolved.
        await fileStorage.DidNotReceive().OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Anonymous_request_is_rejected_with_Unauthorized()
    {
        var repo = Substitute.For<IProviderDocumentRepository>();
        var fileStorage = Substitute.For<IFileStorageService>();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(false);
        currentUser.UserId.Returns((Guid?)null);

        var handler = new DownloadProviderDocumentQueryHandler(
            repo, fileStorage, currentUser, NullLogger<DownloadProviderDocumentQueryHandler>.Instance);

        var result = await handler.Handle(new DownloadProviderDocumentQuery(DocumentId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        // Anonymous callers never touch the repository or storage.
        await repo.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await repo.DidNotReceive().GetTourGuideIdByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await fileStorage.DidNotReceive().OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Download_returns_NotFound_when_file_backing_is_missing()
    {
        var document = BuildDocument();
        var repo = Substitute.For<IProviderDocumentRepository>();
        repo.GetTourGuideIdByUserIdAsync(OwnerUserId, Arg.Any<CancellationToken>())
            .Returns(OwnerTourGuideId);
        repo.GetByIdForTourGuideAsync(DocumentId, OwnerTourGuideId, Arg.Any<CancellationToken>())
            .Returns(document);

        var fileStorage = Substitute.For<IFileStorageService>();
        // Orphaned link / deleted blob: storage cannot open the file.
        fileStorage.OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<FileDownload>.Failure(Error.NotFound("File"), Outcome.NotFound));

        var handler = BuildHandler(repo, OwnerUserId, isAdmin: false, fileStorage: fileStorage);

        var result = await handler.Handle(new DownloadProviderDocumentQuery(DocumentId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
    }

    [Fact]
    public async Task Download_does_not_surface_the_stored_DocumentUrl_or_storage_path()
    {
        var document = BuildDocument();
        var repo = Substitute.For<IProviderDocumentRepository>();
        repo.GetTourGuideIdByUserIdAsync(OwnerUserId, Arg.Any<CancellationToken>())
            .Returns(OwnerTourGuideId);
        repo.GetByIdForTourGuideAsync(DocumentId, OwnerTourGuideId, Arg.Any<CancellationToken>())
            .Returns(document);

        var handler = BuildHandler(repo, OwnerUserId, isAdmin: false, fileBytes: KnownBytes);

        var result = await handler.Handle(new DownloadProviderDocumentQuery(DocumentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // The download result exposes only a sanitized filename derived from OriginalFileName —
        // never the stored DocumentUrl / storage path.
        result.Value!.FileName.Should().Be(KnownFileName);
        result.Value.FileName.Should().NotContain("/uploads/");
        result.Value.FileName.Should().NotContain(SecretDocumentUrl);
        result.Value.FileName.Should().NotContain("provider-documents/");
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static ProviderDocument BuildDocument()
        => ProviderDocument.CreateForTourGuide(
            tourGuideId: OwnerTourGuideId,
            documentType: DocumentType.MoTALicense,
            documentUrl: SecretDocumentUrl,
            originalFileName: KnownFileName,
            expiresAtUtc: DateTime.UtcNow.AddYears(1));

    private static DownloadProviderDocumentQueryHandler BuildHandler(
        IProviderDocumentRepository repo,
        Guid callerUserId,
        bool isAdmin,
        byte[]? fileBytes = null,
        IFileStorageService? fileStorage = null)
    {
        var storage = fileStorage ?? Substitute.For<IFileStorageService>();
        if (fileBytes is not null)
        {
            storage.OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Result<FileDownload>.Success(
                    new FileDownload(new MemoryStream(fileBytes, writable: false), KnownContentType, fileBytes.Length)));
        }

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(callerUserId);
        currentUser.HasPermission($"{BookingFeatures.AdminBookingDashboard}.{AppAction.Read}")
            .Returns(isAdmin);

        return new DownloadProviderDocumentQueryHandler(
            repo, storage, currentUser, NullLogger<DownloadProviderDocumentQueryHandler>.Instance);
    }

    private static async Task<byte[]> ReadAllAsync(Stream stream)
    {
        await using (stream)
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return ms.ToArray();
        }
    }
}
