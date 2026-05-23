using System.Reflection;
using Booking.Application.Caching;
using Booking.Application.Commands.UpdateProviderDocument;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Tests.Unit.Commands;

public sealed class UpdateProviderDocumentHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TourGuideId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Returns_not_found_when_doc_belongs_to_a_different_tour_guide()
    {
        var (handler, repo, _, _, _, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(UserId);
        repo.GetTourGuideIdByUserIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(TourGuideId);
        var foreignDoc = CreatePendingDoc(Guid.NewGuid());
        repo.GetByIdTrackedAsync(foreignDoc.Id, Arg.Any<CancellationToken>()).Returns(foreignDoc);

        var result = await handler.Handle(
            new UpdateProviderDocumentCommand(
                Id: foreignDoc.Id,
                FileStream: null,
                FileName: null,
                ContentType: null,
                FileSize: null,
                ExpiresAt: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
                RowVersion: []),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(e => e.Code == "ProviderDocument.NotFound");
    }

    [Fact]
    public async Task Successfully_updates_expiry_only_and_invalidates_cache()
    {
        var (handler, repo, _, uow, cache, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(UserId);
        repo.GetTourGuideIdByUserIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(TourGuideId);
        var doc = CreatePendingDoc(TourGuideId);
        repo.GetByIdTrackedAsync(doc.Id, Arg.Any<CancellationToken>()).Returns(doc);

        var newExpiry = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2));
        var result = await handler.Handle(
            new UpdateProviderDocumentCommand(
                Id: doc.Id,
                FileStream: null, FileName: null, ContentType: null, FileSize: null,
                ExpiresAt: newExpiry,
                RowVersion: []),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        doc.ExpiresAt!.Value.Date.Should().Be(newExpiry.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).Date);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await cache.Received().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_update_of_expired_document()
    {
        var (handler, repo, _, uow, _, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(UserId);
        repo.GetTourGuideIdByUserIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(TourGuideId);
        var doc = CreatePendingDoc(TourGuideId);
        doc.MarkExpired(DateTime.UtcNow);
        repo.GetByIdTrackedAsync(doc.Id, Arg.Any<CancellationToken>()).Returns(doc);

        var result = await handler.Handle(
            new UpdateProviderDocumentCommand(
                Id: doc.Id,
                FileStream: null, FileName: null, ContentType: null, FileSize: null,
                ExpiresAt: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
                RowVersion: []),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Empty_update_does_not_pass_validator()
    {
        var validator = new UpdateProviderDocumentCommandValidator();
        var cmd = new UpdateProviderDocumentCommand(
            Id: Guid.NewGuid(),
            FileStream: null, FileName: null, ContentType: null, FileSize: null,
            ExpiresAt: null,
            RowVersion: []);

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("At least one", StringComparison.Ordinal));
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Successful_update_invalidates_provider_user_and_document_tags()
    {
        // BOOKING-P1-CACHE-STANDARD-FIX-001 §3: Update MUST evict
        //   • ProviderTag(tourGuideId)  — covers any TourGuide-keyed cache entries
        //   • UserTag(currentUser.UserId) — covers the provider-self list query
        //   • DocumentTag(document.Id) — covers the GET-by-id detail query
        var (handler, repo, _, uow, cache, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(UserId);
        repo.GetTourGuideIdByUserIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(TourGuideId);
        var doc = CreatePendingDoc(TourGuideId);
        repo.GetByIdTrackedAsync(doc.Id, Arg.Any<CancellationToken>()).Returns(doc);

        var newExpiry = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2));
        var result = await handler.Handle(
            new UpdateProviderDocumentCommand(
                Id: doc.Id,
                FileStream: null, FileName: null, ContentType: null, FileSize: null,
                ExpiresAt: newExpiry,
                RowVersion: []),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            BookingProviderDocumentCacheKeys.ProviderTag(TourGuideId), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            BookingProviderDocumentCacheKeys.UserTag(UserId), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            BookingProviderDocumentCacheKeys.DocumentTag(doc.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_does_not_invalidate_cache_when_doc_belongs_to_a_different_tour_guide()
    {
        // Post-SaveChanges-only invariant: 404 path must not touch the cache.
        var (handler, repo, _, _, cache, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(UserId);
        repo.GetTourGuideIdByUserIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(TourGuideId);
        var foreignDoc = CreatePendingDoc(Guid.NewGuid());
        repo.GetByIdTrackedAsync(foreignDoc.Id, Arg.Any<CancellationToken>()).Returns(foreignDoc);

        var result = await handler.Handle(
            new UpdateProviderDocumentCommand(
                Id: foreignDoc.Id,
                FileStream: null, FileName: null, ContentType: null, FileSize: null,
                ExpiresAt: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
                RowVersion: []),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static ProviderDocument CreatePendingDoc(Guid tourGuideId)
    {
        var doc = ProviderDocument.CreateForTourGuide(
            tourGuideId: tourGuideId,
            documentType: DocumentType.MoTALicense,
            documentUrl: "/uploads/orig.pdf",
            originalFileName: "orig.pdf",
            expiresAtUtc: DateTime.UtcNow.AddYears(1));
        doc.ClearDomainEvents();
        return doc;
    }

    private static (
        UpdateProviderDocumentCommandHandler Handler,
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

        var handler = new UpdateProviderDocumentCommandHandler(
            documentRepository: repo,
            fileStorageService: fileStorage,
            unitOfWork: uow,
            currentUser: currentUser,
            cache: cache,
            logger: NullLogger<UpdateProviderDocumentCommandHandler>.Instance);

        return (handler, repo, fileStorage, uow, cache, currentUser);
    }
}
