using Booking.Application.Caching;
using Booking.Application.Queries.GetProviderDocumentById;
using Booking.Application.Queries.GetProviderDocuments;
using Booking.Contracts.Authorization;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Tests.Unit.Queries;

public sealed class ProviderDocumentQueryTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TourGuideId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid AdminUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task GetProviderDocumentsQuery_returns_only_documents_owned_by_caller()
    {
        var repo = Substitute.For<IProviderDocumentRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(UserId);
        repo.GetTourGuideIdByUserIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(TourGuideId);
        var mine = CreateDoc(TourGuideId);
        repo.GetForTourGuideAsync(TourGuideId, Arg.Any<CancellationToken>())
            .Returns(new List<ProviderDocument> { mine });

        var handler = new GetProviderDocumentsQueryHandler(
            repo,
            currentUser,
            NullLogger<GetProviderDocumentsQueryHandler>.Instance);

        var result = await handler.Handle(new GetProviderDocumentsQuery(UserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(d => d.Id == mine.Id);
    }

    [Fact]
    public async Task GetProviderDocumentsQuery_returns_unauthorized_when_userId_mismatches_caller()
    {
        var repo = Substitute.For<IProviderDocumentRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(UserId);

        var handler = new GetProviderDocumentsQueryHandler(
            repo, currentUser, NullLogger<GetProviderDocumentsQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetProviderDocumentsQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Unauthorized);
    }

    [Fact]
    public async Task GetProviderDocumentByIdQuery_returns_404_for_non_owner_non_admin()
    {
        var repo = Substitute.For<IProviderDocumentRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(UserId);
        currentUser.HasPermission(Arg.Any<string>()).Returns(false);
        repo.GetTourGuideIdByUserIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(TourGuideId);
        repo.GetByIdForTourGuideAsync(Arg.Any<Guid>(), TourGuideId, Arg.Any<CancellationToken>())
            .Returns((ProviderDocument?)null);

        var handler = new GetProviderDocumentByIdQueryHandler(
            repo, currentUser, NullLogger<GetProviderDocumentByIdQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetProviderDocumentByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
    }

    [Fact]
    public async Task GetProviderDocumentByIdQuery_admin_override_uses_Read_permission_not_Update()
    {
        var repo = Substitute.For<IProviderDocumentRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(AdminUserId);
        currentUser.HasPermission(
                $"{BookingFeatures.AdminBookingDashboard}.{AppAction.Read}")
            .Returns(true);
        currentUser.HasPermission(
                $"{BookingFeatures.AdminBookingDashboard}.{AppAction.Update}")
            .Returns(false);

        var foreignDoc = CreateDoc(Guid.NewGuid()); // owned by a different tour guide
        repo.GetByIdAsync(foreignDoc.Id, Arg.Any<CancellationToken>()).Returns(foreignDoc);

        var handler = new GetProviderDocumentByIdQueryHandler(
            repo, currentUser, NullLogger<GetProviderDocumentByIdQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetProviderDocumentByIdQuery(foreignDoc.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(foreignDoc.Id);
        // Crucially: ownership lookup never executed because admin path short-circuited.
        await repo.DidNotReceive().GetByIdForTourGuideAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ── BOOKING-P1-CACHE-STANDARD-FIX-001 §3 — cache-shape contracts ─────────

    [Fact]
    public void GetProviderDocumentsQuery_publishes_user_scoped_cache_key_and_tag()
    {
        var query = new GetProviderDocumentsQuery(UserId);

        query.CacheKey.Should().Be(BookingProviderDocumentCacheKeys.UserListKey(UserId));
        query.Tags.Should().ContainSingle()
            .Which.Should().Be(BookingProviderDocumentCacheKeys.UserTag(UserId));
        query.CacheDuration.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void GetProviderDocumentByIdQuery_publishes_document_scoped_cache_key_and_tag()
    {
        var docId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var query = new GetProviderDocumentByIdQuery(docId);

        query.CacheKey.Should().Be(BookingProviderDocumentCacheKeys.ByIdKey(docId));
        query.Tags.Should().ContainSingle()
            .Which.Should().Be(BookingProviderDocumentCacheKeys.DocumentTag(docId));
        query.CacheDuration.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void ProviderDocument_cache_key_factories_are_deterministic_and_distinct()
    {
        var u = Guid.NewGuid();
        var g = Guid.NewGuid();
        var d = Guid.NewGuid();

        // Deterministic — repeated calls return the same string.
        BookingProviderDocumentCacheKeys.UserTag(u).Should().Be(BookingProviderDocumentCacheKeys.UserTag(u));
        BookingProviderDocumentCacheKeys.ProviderTag(g).Should().Be(BookingProviderDocumentCacheKeys.ProviderTag(g));
        BookingProviderDocumentCacheKeys.DocumentTag(d).Should().Be(BookingProviderDocumentCacheKeys.DocumentTag(d));

        // Distinct shapes — UserTag must NOT collide with ProviderTag for the same Guid value.
        BookingProviderDocumentCacheKeys.UserTag(u).Should().NotBe(BookingProviderDocumentCacheKeys.ProviderTag(u));
        BookingProviderDocumentCacheKeys.UserListKey(u).Should().NotBe(BookingProviderDocumentCacheKeys.UserTag(u));
    }

    private static ProviderDocument CreateDoc(Guid tourGuideId)
        => ProviderDocument.CreateForTourGuide(
            tourGuideId: tourGuideId,
            documentType: DocumentType.MoTALicense,
            documentUrl: "/uploads/d.pdf",
            originalFileName: "d.pdf",
            expiresAtUtc: DateTime.UtcNow.AddYears(1));
}
