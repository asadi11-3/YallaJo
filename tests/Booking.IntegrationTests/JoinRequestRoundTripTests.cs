using System.Globalization;
using System.Reflection;
using Booking.Application.Commands.JoinRequest.ApproveJoinRequest;
using Booking.Application.Commands.JoinRequest.CreateJoinRequest;
using Booking.Application.Commands.JoinRequest.RejectJoinRequest;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Repositories;
using Booking.Domain.ValueObjects;
using Booking.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;

namespace Booking.IntegrationTests;

/// <summary>
/// SQLite-in-memory roundtrip for TASK 6 JoinRequest. Verifies create/approve/reject
/// against a real EF provider, including the slot capacity mutation on approval.
/// <para>
/// <b>Important:</b> SQLite ignores the SQL-Server-only filtered-index syntax
/// (<c>[Status] = 0 AND [IsDeleted] = 0</c>), so the duplicate-pending invariant is
/// asserted here at the <i>application layer</i> via
/// <c>IJoinRequestRepository.ExistsPendingForUserAndBookingAsync</c>. The DB-side
/// filtered unique index <c>UX_JoinRequests_Pending_TourBookingId_UserId</c> is the
/// production safety net.
/// </para>
/// </summary>
public sealed class JoinRequestRoundTripTests
{
    private static readonly Guid TourId     = Guid.NewGuid();
    private static readonly Guid ProviderId = Guid.NewGuid();

    [Fact]
    public async Task Create_then_Approve_persists_approved_status_and_increases_slot_booked_count()
    {
        var (context, jrRepo, bookingRepo, slotRepo) = NewContext();
        var ownerId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var (booking, slot) = await SeedBookingAndSlotAsync(context, ownerId);

        // Create as requester
        var createHandler = NewCreateHandler(jrRepo, bookingRepo, slotRepo, context, requesterId);
        var createResult = await createHandler.Handle(
            new CreateJoinRequestCommand(booking.Id, ParticipantCount: 2, Message: "Can I join?"),
            CancellationToken.None);
        createResult.IsSuccess.Should().BeTrue();
        createResult.Outcome.Should().Be(YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome.Created);

        var jrId = createResult.Value!.Id;
        context.ChangeTracker.Clear();

        // Approve as booking owner
        var approveHandler = NewApproveHandler(jrRepo, bookingRepo, slotRepo, context, ownerId);
        var approveResult = await approveHandler.Handle(
            new ApproveJoinRequestCommand(jrId),
            CancellationToken.None);
        approveResult.IsSuccess.Should().BeTrue();

        context.ChangeTracker.Clear();
        var savedJr = await context.JoinRequests.AsNoTracking().FirstAsync(j => j.Id == jrId);
        savedJr.Status.Should().Be(JoinRequestStatus.Approved);
        savedJr.RespondedAt.Should().NotBeNull();

        var savedSlot = await context.AvailabilitySlots.AsNoTracking().FirstAsync(s => s.Id == slot.Id);
        savedSlot.BookedCount.Should().Be(2, "join request capacity must be reserved on the slot");
    }

    [Fact]
    public async Task Create_then_Reject_persists_rejected_status_and_response_message()
    {
        var (context, jrRepo, bookingRepo, slotRepo) = NewContext();
        var ownerId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var (booking, _) = await SeedBookingAndSlotAsync(context, ownerId);

        var createHandler = NewCreateHandler(jrRepo, bookingRepo, slotRepo, context, requesterId);
        var createResult = await createHandler.Handle(
            new CreateJoinRequestCommand(booking.Id, 1, null),
            CancellationToken.None);
        createResult.IsSuccess.Should().BeTrue();
        var jrId = createResult.Value!.Id;
        context.ChangeTracker.Clear();

        var rejectHandler = NewRejectHandler(jrRepo, bookingRepo, context, ownerId);
        var rejectResult = await rejectHandler.Handle(
            new RejectJoinRequestCommand(jrId, "Already at full headcount."),
            CancellationToken.None);
        rejectResult.IsSuccess.Should().BeTrue();

        context.ChangeTracker.Clear();
        var savedJr = await context.JoinRequests.AsNoTracking().FirstAsync(j => j.Id == jrId);
        savedJr.Status.Should().Be(JoinRequestStatus.Rejected);
        savedJr.ResponseMessage.Should().Be("Already at full headcount.");
    }

    [Fact]
    public async Task Duplicate_pending_create_returns_Conflict()
    {
        var (context, jrRepo, bookingRepo, slotRepo) = NewContext();
        var ownerId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var (booking, _) = await SeedBookingAndSlotAsync(context, ownerId);

        var createHandler = NewCreateHandler(jrRepo, bookingRepo, slotRepo, context, requesterId);

        var first = await createHandler.Handle(
            new CreateJoinRequestCommand(booking.Id, 1, null),
            CancellationToken.None);
        first.IsSuccess.Should().BeTrue();
        context.ChangeTracker.Clear();

        var second = await createHandler.Handle(
            new CreateJoinRequestCommand(booking.Id, 1, null),
            CancellationToken.None);
        second.IsFailure.Should().BeTrue();
        second.Outcome.Should().Be(YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome.Conflict);
        second.Errors.Should().ContainSingle(e => e.Code == "JoinRequest.Duplicate");
    }

    [Fact]
    public async Task Self_join_is_forbidden()
    {
        var (context, jrRepo, bookingRepo, slotRepo) = NewContext();
        var ownerId = Guid.NewGuid();
        var (booking, _) = await SeedBookingAndSlotAsync(context, ownerId);

        var handler = NewCreateHandler(jrRepo, bookingRepo, slotRepo, context, ownerId); // requester == owner
        var result = await handler.Handle(
            new CreateJoinRequestCommand(booking.Id, 1, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == "JoinRequest.SelfJoin");
    }

    // ── Test infrastructure ───────────────────────────────────────────────────

    private static (BookingDbContext context,
        IJoinRequestRepository jrRepo,
        ITourBookingRepository bookingRepo,
        IAvailabilitySlotRepository slotRepo) NewContext()
    {
        var conn = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseSqlite(conn)
            .Options;
        var ctx = new BookingDbContext(options);
        ctx.Database.EnsureCreated();

        var jrRepo = (IJoinRequestRepository)CreateInternalRepo("Booking.Infrastructure.Repositories.JoinRequestRepository", ctx);
        var bookingRepo = (ITourBookingRepository)CreateInternalRepo("Booking.Infrastructure.Repositories.TourBookingRepository", ctx);
        var slotRepo = (IAvailabilitySlotRepository)CreateInternalRepo("Booking.Infrastructure.Repositories.AvailabilitySlotRepository", ctx);
        return (ctx, jrRepo, bookingRepo, slotRepo);
    }

    private static object CreateInternalRepo(string fullTypeName, BookingDbContext ctx)
    {
        var type = typeof(Booking.Infrastructure.DependencyInjection).Assembly.GetType(fullTypeName)
            ?? throw new InvalidOperationException($"Could not locate internal repository type '{fullTypeName}'.");
        return Activator.CreateInstance(
            type,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [ctx],
            culture: CultureInfo.InvariantCulture)
            ?? throw new InvalidOperationException($"Failed to instantiate '{fullTypeName}'.");
    }

    private static async Task<(TourBooking booking, AvailabilitySlot slot)> SeedBookingAndSlotAsync(
        BookingDbContext ctx,
        Guid ownerUserId)
    {
        // Seed a TourGuide row so the AvailabilitySlot.TourGuideId FK is satisfied.
        var guide = (TourGuide)Activator.CreateInstance(typeof(TourGuide), nonPublic: true)!;
        SetProp(guide, nameof(TourGuide.Id), Guid.NewGuid());
        SetProp(guide, nameof(TourGuide.UserId), ownerUserId);
        SetProp(guide, nameof(TourGuide.IsActive), true);
        ctx.TourGuides.Add(guide);

        var slot = AvailabilitySlot.CreateForTour(
            tourGuideId: guide.Id,
            tourId: TourId,
            date: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7),
            start: new TimeOnly(9, 0),
            end: new TimeOnly(11, 0),
            maxCapacity: 20);
        ctx.AvailabilitySlots.Add(slot);
        await ctx.SaveChangesAsync();

        var pricing = new BookingPricing(
            Subtotal: 90m, DiscountAmount: 0m, LoyaltyAmount: 0m, TotalAmount: 90m,
            CommissionRate: 0.10m, CommissionAmount: 9m, Currency: "JOD",
            LineItems: new List<BookingLineItem> { new(TierType.Adult, 1, 90m, "JOD") });
        var booking = TourBooking.Create(
            userId: ownerUserId,
            tourId: TourId,
            providerId: ProviderId,
            availabilitySlotId: slot.Id,
            participantCount: 1,
            pricing: pricing,
            reference: BookingReference.Compose(new DateOnly(2026, 7, 1), "JRAAAA"),
            refundPolicySnapshot: "{}",
            isInstantBooking: true,
            paymentExpiresAt: DateTime.UtcNow.AddMinutes(10),
            lineItemsJson: "[]");
        booking.Confirm(ConfirmationSource.PaymentWebhook);

        ctx.TourBookings.Add(booking);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();
        return (booking, slot);
    }

    private static void SetProp<TValue>(object target, string name, TValue value)
    {
        var prop = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property '{name}' not found on {target.GetType().Name}.");
        prop.SetValue(target, value);
    }

    private static CreateJoinRequestCommandHandler NewCreateHandler(
        IJoinRequestRepository jr,
        ITourBookingRepository bookings,
        IAvailabilitySlotRepository slots,
        BookingDbContext ctx,
        Guid requesterId)
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns(requesterId);
        return new CreateJoinRequestCommandHandler(
            jr, bookings, slots,
            new TestBookingUnitOfWork(ctx),
            user,
            NullLogger<CreateJoinRequestCommandHandler>.Instance);
    }

    private static ApproveJoinRequestCommandHandler NewApproveHandler(
        IJoinRequestRepository jr,
        ITourBookingRepository bookings,
        IAvailabilitySlotRepository slots,
        BookingDbContext ctx,
        Guid viewerId)
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns(viewerId);
        return new ApproveJoinRequestCommandHandler(
            jr, bookings, slots,
            new TestBookingUnitOfWork(ctx),
            user,
            NullLogger<ApproveJoinRequestCommandHandler>.Instance);
    }

    private static RejectJoinRequestCommandHandler NewRejectHandler(
        IJoinRequestRepository jr,
        ITourBookingRepository bookings,
        BookingDbContext ctx,
        Guid viewerId)
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns(viewerId);
        return new RejectJoinRequestCommandHandler(
            jr, bookings,
            new TestBookingUnitOfWork(ctx),
            user,
            NullLogger<RejectJoinRequestCommandHandler>.Instance);
    }

    private sealed class TestBookingUnitOfWork(BookingDbContext context) : IBookingUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => context.SaveChangesAsync(cancellationToken);
    }
}
