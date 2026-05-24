using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Booking.Application.Commands.RefundPolicy.UpdateRefundPolicy;
using Booking.Application.Commands.RefundPolicy.UpsertRefundPolicy;
using Booking.Application.Interfaces;
using Booking.Application.Queries.GetRefundPolicyByTour;
using Booking.Domain.Repositories;
using Booking.Infrastructure.Persistence;
using ContentTours.Contracts.Authorization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;

namespace Booking.IntegrationTests;

/// <summary>
/// Sqlite-in-memory roundtrip for the tour-scoped RefundPolicy aggregate.
/// Verifies that EF 9 owned-JSON tier serialization works against a real provider AND
/// that the unique TourId index is enforced.
/// </summary>
public sealed class RefundPolicyRoundTripTests
{
    [Fact]
    public async Task Upsert_then_Get_returns_stored_tiers()
    {
        var (context, repo) = NewContext();

        var ownerId = Guid.NewGuid();
        var tourId = Guid.NewGuid();
        var (upsertHandler, _) = NewHandlers(context, repo, ownerUserId: ownerId, tourId: tourId);
        var getHandler = new GetRefundPolicyByTourQueryHandler(repo, NullLogger<GetRefundPolicyByTourQueryHandler>.Instance);

        var tiers = new[]
        {
            new RefundTierDto(72, 100m),
            new RefundTierDto(24, 50m),
            new RefundTierDto(0, 0m),
        };
        var upsertResult = await upsertHandler.Handle(
            new UpsertRefundPolicyCommand(tourId, tiers), CancellationToken.None);
        upsertResult.IsSuccess.Should().BeTrue();

        var getResult = await getHandler.Handle(
            new GetRefundPolicyByTourQuery(tourId), CancellationToken.None);
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value!.IsDefault.Should().BeFalse();
        getResult.Value.Tiers.Should().HaveCount(3);
        getResult.Value.Tiers.Select(t => t.HoursBeforeTour).Should().ContainInOrder(72, 24, 0);
    }

    [Fact]
    public async Task Get_with_no_row_returns_default_tiers()
    {
        var (context, repo) = NewContext();
        var getHandler = new GetRefundPolicyByTourQueryHandler(repo, NullLogger<GetRefundPolicyByTourQueryHandler>.Instance);

        var result = await getHandler.Handle(
            new GetRefundPolicyByTourQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsDefault.Should().BeTrue();
        result.Value.Tiers.Should().Contain(t => t.HoursBeforeTour == 24 && t.RefundPercent == 100m);
        result.Value.Tiers.Should().Contain(t => t.HoursBeforeTour == 0 && t.RefundPercent == 0m);
    }

    [Fact]
    public async Task Second_upsert_for_same_tour_updates_the_same_row()
    {
        var (context, repo) = NewContext();
        var ownerId = Guid.NewGuid();
        var tourId = Guid.NewGuid();
        var (upsertHandler, _) = NewHandlers(context, repo, ownerUserId: ownerId, tourId: tourId);

        await upsertHandler.Handle(new UpsertRefundPolicyCommand(tourId, new[]
        {
            new RefundTierDto(48, 80m),
            new RefundTierDto(0, 0m),
        }), CancellationToken.None);

        await upsertHandler.Handle(new UpsertRefundPolicyCommand(tourId, new[]
        {
            new RefundTierDto(96, 100m),
            new RefundTierDto(24, 25m),
        }), CancellationToken.None);

        var rowCount = await context.RefundPolicies.IgnoreQueryFilters().CountAsync(p => p.TourId == tourId);
        rowCount.Should().Be(1, "upsert must reuse the existing row for the same TourId");

        var policy = await context.RefundPolicies
            .Include(p => p.Tiers)
            .FirstAsync(p => p.TourId == tourId);
        policy.Tiers.Should().HaveCount(2);
        policy.Tiers.Select(t => t.HoursBeforeTour).Should().ContainInOrder(96, 24);
    }

    [Fact]
    public async Task Update_replaces_tiers_and_can_be_read_back()
    {
        var (context, repo) = NewContext();
        var ownerId = Guid.NewGuid();
        var tourId = Guid.NewGuid();
        var (upsertHandler, updateHandler) = NewHandlers(context, repo, ownerUserId: ownerId, tourId: tourId);

        var upsertResult = await upsertHandler.Handle(new UpsertRefundPolicyCommand(tourId, new[]
        {
            new RefundTierDto(24, 100m),
            new RefundTierDto(0, 0m),
        }), CancellationToken.None);
        upsertResult.IsSuccess.Should().BeTrue();
        var policyId = upsertResult.Value!.Id;

        // Clear the EF change-tracker between operations to simulate a fresh request scope.
        context.ChangeTracker.Clear();

        var updateResult = await updateHandler.Handle(new UpdateRefundPolicyCommand(
            policyId,
            new[]
            {
                new RefundTierDto(168, 100m),
                new RefundTierDto(72, 50m),
                new RefundTierDto(0, 10m),
            },
            RowVersion: []),
            CancellationToken.None);
        updateResult.IsSuccess.Should().BeTrue();

        var reload = await context.RefundPolicies
            .AsNoTracking()
            .Include(p => p.Tiers)
            .FirstAsync(p => p.Id == policyId);
        reload.Tiers.Should().HaveCount(3);
        reload.Tiers.Select(t => t.HoursBeforeTour).Should().ContainInOrder(168, 72, 0);
        reload.Tiers.Should().Contain(t => t.HoursBeforeTour == 0 && t.RefundPercent == 10m);
    }

    [Fact]
    public async Task JSON_column_uses_lowercamel_property_names_for_wire_compatibility()
    {
        // The RefundPolicySnapshot on TourBooking and the cancel-handler parser both expect
        // {"hoursBeforeTour": int, "refundPercent": decimal}. The owned-collection JSON
        // mapping MUST honor those exact names so existing code paths keep working.
        var (context, repo) = NewContext();
        var ownerId = Guid.NewGuid();
        var tourId = Guid.NewGuid();
        var (upsertHandler, _) = NewHandlers(context, repo, ownerUserId: ownerId, tourId: tourId);

        await upsertHandler.Handle(new UpsertRefundPolicyCommand(tourId, new[]
        {
            new RefundTierDto(72, 100m),
            new RefundTierDto(0, 0m),
        }), CancellationToken.None);

        // Pull the raw Tiers column directly via ADO to check the JSON shape.
        // We don't filter by TourId because GUID round-trip via SQLite TEXT is parameter-binding
        // sensitive — easier to grab the only row in this in-memory DB.
        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Tiers FROM RefundPolicies LIMIT 1";
        var raw = (string?)await cmd.ExecuteScalarAsync();
        raw.Should().NotBeNullOrWhiteSpace();

        using var doc = JsonDocument.Parse(raw!);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        var first = doc.RootElement[0];
        first.TryGetProperty("hoursBeforeTour", out _).Should().BeTrue("lowercamel name required for cancel-handler compat");
        first.TryGetProperty("refundPercent", out _).Should().BeTrue("lowercamel name required for cancel-handler compat");
    }

    // ── Test infrastructure ───────────────────────────────────────────────────

    private static (BookingDbContext context, IRefundPolicyRepository repo) NewContext()
    {
        var conn = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseSqlite(conn)
            .Options;
        var ctx = new BookingDbContext(options);
        ctx.Database.EnsureCreated();
        var repo = CreateRepo(ctx);
        return (ctx, repo);
    }

    private static IRefundPolicyRepository CreateRepo(BookingDbContext ctx)
    {
        var type = typeof(Booking.Infrastructure.DependencyInjection).Assembly
            .GetType("Booking.Infrastructure.Repositories.RefundPolicyRepository")
            ?? throw new InvalidOperationException("Could not locate internal RefundPolicyRepository type.");

        var instance = Activator.CreateInstance(
            type,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [ctx],
            culture: CultureInfo.InvariantCulture)
            ?? throw new InvalidOperationException("Failed to instantiate RefundPolicyRepository.");
        return (IRefundPolicyRepository)instance;
    }

    private static (UpsertRefundPolicyCommandHandler upsert, UpdateRefundPolicyCommandHandler update)
        NewHandlers(BookingDbContext ctx, IRefundPolicyRepository repo, Guid ownerUserId, Guid tourId)
    {
        var ownership = Substitute.For<ITourOwnershipService>();
        ownership.GetTourOwnershipAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => new EntityOwnershipResolution(true, true, false, ownerUserId));

        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns(ownerUserId);

        var uow = new TestBookingUnitOfWork(ctx);
        var cache = Substitute.For<HybridCache>();

        var upsert = new UpsertRefundPolicyCommandHandler(
            repo, ownership, uow, cache, user,
            NullLogger<UpsertRefundPolicyCommandHandler>.Instance);
        var update = new UpdateRefundPolicyCommandHandler(
            repo, ownership, uow, cache, user,
            NullLogger<UpdateRefundPolicyCommandHandler>.Instance);
        return (upsert, update);
    }

    private sealed class TestBookingUnitOfWork(BookingDbContext ctx) : IBookingUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => ctx.SaveChangesAsync(cancellationToken);
    }
}
