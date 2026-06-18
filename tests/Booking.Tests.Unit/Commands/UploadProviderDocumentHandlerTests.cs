using Booking.Application.Caching;
using Booking.Application.Commands.UploadProviderDocument;
using Booking.Application.Interfaces;
using Booking.Domain.Enums;
using Booking.Domain.Extensions;
using Booking.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Tests.Unit.Commands;

public sealed class UploadProviderDocumentHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TourGuideId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Returns_unauthorized_when_caller_is_anonymous()
    {
        var (handler, _, _, _, _, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(false);

        var result = await handler.Handle(BuildCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        result.Errors.Should().ContainSingle(e => e.Code == "ProviderDocument.Unauthorized");
    }

    [Fact]
    public async Task Returns_forbidden_when_user_is_not_a_tour_guide()
    {
        var (handler, repo, _, _, _, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(UserId);
        repo.GetTourGuideIdByUserIdAsync(UserId, Arg.Any<CancellationToken>()).Returns((Guid?)null);

        var result = await handler.Handle(BuildCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == "ProviderDocument.NotAProvider");
    }

    [Fact]
    public async Task Returns_invalid_when_file_signature_does_not_match_content_type()
    {
        var (handler, repo, _, _, _, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(UserId);
        repo.GetTourGuideIdByUserIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(TourGuideId);

        // Random bytes — no recognised magic-byte signature.
        var stream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04 });
        var cmd = BuildCommand(stream: stream, contentType: "application/pdf", fileName: "fake.pdf");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "ProviderDocument.UnsupportedType");
    }

    [Fact]
    public async Task Returns_conflict_when_active_doc_of_same_type_already_exists()
    {
        var (handler, repo, fileStorage, _, _, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(UserId);
        repo.GetTourGuideIdByUserIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(TourGuideId);
        repo.ExistsActiveTypeForTourGuideAsync(
                TourGuideId,
                DocumentType.MoTALicense,
                null,
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await handler.Handle(BuildCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "ProviderDocument.DuplicateType");
        // File storage MUST NOT be touched once duplicate is detected.
        await fileStorage.DidNotReceive().UploadAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Successful_pdf_upload_under_10MB_returns_Created_dto_and_invalidates_cache()
    {
        var (handler, repo, fileStorage, uow, cache, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(UserId);
        repo.GetTourGuideIdByUserIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(TourGuideId);
        repo.ExistsActiveTypeForTourGuideAsync(
                TourGuideId, DocumentType.MoTALicense, null, Arg.Any<CancellationToken>())
            .Returns(false);
        fileStorage.UploadAsync(
                Arg.Any<Stream>(), "mota.pdf", "application/pdf", "provider-documents", Arg.Any<CancellationToken>())
            .Returns(new FileUploadResult("/uploads/provider-documents/mota.pdf", "key", 1024));

        var stream = new MemoryStream(PdfBytes(8 * 1024)); // 8 KB
        var cmd = BuildCommand(stream: stream, fileSize: stream.Length);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Created);
        result.Value!.Type.Should().Be(DocumentType.MoTALicense);
        result.Value.Status.Should().Be(DocumentStatus.Pending);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await repo.Received(1).AddAsync(Arg.Any<Booking.Domain.Entities.ProviderDocument>(), Arg.Any<CancellationToken>());

        // BOOKING-P1-CACHE-STANDARD-FIX-001 §3: write handler MUST evict both the TourGuide-scoped
        // provider tag AND the user-scoped tag the provider-self list query relies on.
        await cache.Received(1).RemoveByTagAsync(
            BookingProviderDocumentCacheKeys.ProviderTag(TourGuideId), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            BookingProviderDocumentCacheKeys.UserTag(UserId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upload_does_not_invalidate_cache_on_duplicate_conflict()
    {
        // Sanity check: when the duplicate-type guard rejects the upload BEFORE storage,
        // no cache eviction should run (per the post-SaveChanges-only rule).
        var (handler, repo, fileStorage, _, cache, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(UserId);
        repo.GetTourGuideIdByUserIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(TourGuideId);
        repo.ExistsActiveTypeForTourGuideAsync(
                TourGuideId, DocumentType.MoTALicense, null, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await handler.Handle(BuildCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await fileStorage.DidNotReceive().UploadAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task File_at_max_size_boundary_passes_validation()
    {
        // 10 MB boundary — inclusive.
        const int bytes = 10 * 1024 * 1024;
        bytes.Should().Be(DocumentTypeExtensions.MaxUploadBytes);

        var validator = new UploadProviderDocumentCommandValidator();
        var cmd = BuildCommand(stream: new MemoryStream(PdfBytes(1)), fileSize: bytes);
        var validation = validator.Validate(cmd);
        validation.IsValid.Should().BeTrue(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void Validator_rejects_files_larger_than_10MB()
    {
        const int oversize = (10 * 1024 * 1024) + 1;
        var validator = new UploadProviderDocumentCommandValidator();
        var cmd = BuildCommand(stream: new MemoryStream(PdfBytes(1)), fileSize: oversize);

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("MB limit", StringComparison.Ordinal));
    }

    [Fact]
    public void Validator_rejects_docx_content_type()
    {
        var validator = new UploadProviderDocumentCommandValidator();
        var cmd = BuildCommand(
            stream: new MemoryStream(PdfBytes(1)),
            contentType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            fileName: "doc.docx");

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("PDF, JPEG, and PNG", StringComparison.Ordinal));
    }

    [Fact]
    public void Validator_rejects_MoTALicense_without_ExpiresAt()
    {
        var validator = new UploadProviderDocumentCommandValidator();
        var stream = new MemoryStream(PdfBytes(64));
        var cmd = new UploadProviderDocumentCommand(
            Type: DocumentType.MoTALicense,
            FileStream: stream,
            FileName: "mota.pdf",
            ContentType: "application/pdf",
            FileSize: stream.Length,
            ExpiresAt: null);

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("requires an ExpiresAt", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DB_save_failure_cleans_up_orphan_file()
    {
        var (handler, repo, fileStorage, uow, _, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(UserId);
        repo.GetTourGuideIdByUserIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(TourGuideId);
        repo.ExistsActiveTypeForTourGuideAsync(
                TourGuideId, DocumentType.MoTALicense, null, Arg.Any<CancellationToken>())
            .Returns(false);
        fileStorage.UploadAsync(
                Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new FileUploadResult("/uploads/provider-documents/mota.pdf", "key", 1024));
        uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new Microsoft.EntityFrameworkCore.DbUpdateException("unique-index race"));

        var cmd = BuildCommand(stream: new MemoryStream(PdfBytes(64)));
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        await fileStorage.Received(1).DeleteAsync("/uploads/provider-documents/mota.pdf", Arg.Any<CancellationToken>());
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static byte[] PdfBytes(int payloadLength)
    {
        // "%PDF" header + payload, sufficient for the magic-byte sniffer.
        var header = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var result = new byte[header.Length + payloadLength];
        Buffer.BlockCopy(header, 0, result, 0, header.Length);
        return result;
    }

    private static UploadProviderDocumentCommand BuildCommand(
        DocumentType type = DocumentType.MoTALicense,
        Stream? stream = null,
        string contentType = "application/pdf",
        string fileName = "mota.pdf",
        long? fileSize = null,
        DateOnly? expiresAt = null)
    {
        stream ??= new MemoryStream(PdfBytes(64));
        return new UploadProviderDocumentCommand(
            Type: type,
            FileStream: stream,
            FileName: fileName,
            ContentType: contentType,
            FileSize: fileSize ?? stream.Length,
            ExpiresAt: expiresAt ?? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)));
    }

    private static (
        UploadProviderDocumentCommandHandler Handler,
        IProviderDocumentRepository Repo,
        IFileStorageService FileStorage,
        IBookingUnitOfWork Uow,
        HybridCache Cache,
        ICurrentUser CurrentUser) BuildSubject()
    {
        var repo = Substitute.For<IProviderDocumentRepository>();
        var fileStorage = Substitute.For<IFileStorageService>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();

        var handler = new UploadProviderDocumentCommandHandler(
            documentRepository: repo,
            fileStorageService: fileStorage,
            unitOfWork: uow,
            currentUser: currentUser,
            cache: cache,
            logger: NullLogger<UploadProviderDocumentCommandHandler>.Instance);

        return (handler, repo, fileStorage, uow, cache, currentUser);
    }
}
