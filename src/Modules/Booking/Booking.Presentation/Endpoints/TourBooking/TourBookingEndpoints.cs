using System.Globalization;
using Booking.Application.Commands.CancelTourBooking;
using Booking.Application.Commands.CompleteTourBooking;
using Booking.Application.Commands.ConfirmTourBooking;
using Booking.Application.Commands.RejectTourBooking;
using Booking.Application.Queries.GetAllBookings;
using Booking.Application.Queries.GetMyBookings;
using Booking.Application.Queries.GetProviderBookings;
using Booking.Application.Queries.GetTourBookingById;
using Booking.Contracts.Authorization;
using Booking.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Booking.Presentation.Endpoints.TourBooking;

/// <summary>
/// Tour booking endpoints (TASK 4). Mounted under <c>/api/v1/booking</c>.
/// </summary>
internal static class TourBookingEndpoints
{
    internal static void MapTourBookingEndpoints(RouteGroupBuilder group)
    {
        MapCreateTourBookingEndpoint(group);
        MapGetTourBookingByIdEndpoint(group);
        MapGetMyBookingsEndpoint(group);
        MapGetProviderBookingsEndpoint(group);
        MapGetAllBookingsEndpoint(group);
        MapConfirmTourBookingEndpoint(group);
        MapRejectTourBookingEndpoint(group);
        MapCancelTourBookingEndpoint(group);
        MapCompleteTourBookingEndpoint(group);
    }

    private static void MapCompleteTourBookingEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/complete", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new CompleteTourBookingCommand(id), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("CompleteTourBooking")
            .WithSummary("Provider marks a Confirmed booking as Completed after the tour has started.")
            .WithDescription(
                "Body is empty. Caller must be the provider of the tour or an admin. " +
                "Returns 422 TourBooking.NotYetStarted if invoked before the slot's start time.")
            .Produces<CompleteTourBookingResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.TourBooking, AppAction.Complete))
            .RequireAuthorization();
    }

    private static void MapCancelTourBookingEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/cancel", async (
                Guid id,
                CancelTourBookingRequest? request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = (request ?? new CancelTourBookingRequest(null)).ToCommand(id);
                var result = await sender.Send(command, cancellationToken);
                return result.ToApiResult();
            })
            .WithName("CancelTourBooking")
            .WithSummary("Cancel a booking (user, provider, or admin).")
            .WithDescription(
                "User cancellations follow the booking's refund-policy snapshot. " +
                "Provider/admin cancellations ALWAYS produce a 100% refund and require a reason >= 10 chars. " +
                "Source is auto-detected from the caller's identity and permissions.")
            .Accepts<CancelTourBookingRequest>("application/json")
            .Produces<CancelTourBookingResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.TourBooking, AppAction.Cancel))
            .RequireAuthorization();
    }

    private static void MapRejectTourBookingEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/reject", async (
                Guid id,
                RejectTourBookingRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request.ToCommand(id), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("RejectTourBooking")
            .WithSummary("Provider rejects a PendingConfirmation booking. ALWAYS triggers a 100% automatic refund.")
            .WithDescription(
                "Reason required (10-500 chars). Caller must be the provider of the tour or admin. " +
                "Booking transitions to Rejected; downstream consumers handle the refund via outbox.")
            .Accepts<RejectTourBookingRequest>("application/json")
            .Produces<RejectTourBookingResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.TourBooking, AppAction.Reject))
            .RequireAuthorization();
    }

    private static void MapConfirmTourBookingEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/confirm", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new ConfirmTourBookingCommand(id), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("ConfirmTourBooking")
            .WithSummary("Provider confirms a booking that is in AwaitingPayment or PendingConfirmation state.")
            .WithDescription(
                "Body is empty. Caller must be the provider of the tour, or an admin. " +
                "Transitions the booking to Confirmed and raises a TourBookingConfirmedDomainEvent " +
                "(integration event published via outbox).")
            .Produces<ConfirmTourBookingResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.TourBooking, AppAction.Confirm))
            .RequireAuthorization();
    }

    private static void MapCreateTourBookingEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/tour", async (
                CreateTourBookingRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request.ToCommand(), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("CreateTourBooking")
            .WithSummary("Create a tour booking in AwaitingPayment state.")
            .WithDescription(
                "Validates availability, locks slot capacity, calculates pricing, and emits " +
                "the tour-booking.created.v1 integration event. The booking expires automatically " +
                "if no payment is received within 10 minutes.")
            .Accepts<CreateTourBookingRequest>("application/json")
            .Produces<Application.Commands.CreateTourBooking.CreateTourBookingResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.TourBooking, AppAction.Create))
            .RequireAuthorization();
    }

    private static void MapGetTourBookingByIdEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (
                Guid id,
                ICurrentUser currentUser,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                {
                    return Result.Failure<TourBookingDetailDto>(
                            new Error("TourBooking.Unauthorized", "Authentication is required to view a booking."),
                            Outcome.Unauthorized)
                        .ToApiResult();
                }
        
                var viewerId = currentUser.UserId.Value;
                var isAdmin = currentUser.HasPermission(
                    PermissionPolicyNames.Build(BookingFeatures.AdminBookingDashboard, AppAction.Read));
        
                var query = new GetTourBookingByIdQuery(id, viewerId, isAdmin);
                var result = await sender.Send(query, cancellationToken);
                return result.ToApiResult();
            })
            .WithName("GetTourBookingById")
            .WithSummary("Get a tour booking by id (owner, provider, or admin).")
            .WithDescription(
                "Returns the full booking projection. The caller must be the booking owner, " +
                "the provider of the tour, or hold the AdminBookingDashboard.Read permission. " +
                "Other authenticated callers receive 403 TourBooking.OwnerMismatch.")
            .Produces<TourBookingDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.TourBooking, AppAction.ReadOwn))
            .RequireAuthorization();
    }

    private static void MapGetMyBookingsEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/my-bookings", async (
                ICurrentUser currentUser,
                ISender sender,
                CancellationToken cancellationToken,
                string? status = null,
                string? fromDate = null,
                string? toDate = null,
                Guid? tourId = null,
                string? cursor = null,
                int pageSize = GetMyBookingsQuery.DefaultPageSize,
                bool countTotal = false) =>
            {
                if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                {
                    return Result.Failure<MyBookingsPage>(
                            new Error("TourBooking.Unauthorized", "Authentication is required."),
                            Outcome.Unauthorized)
                        .ToApiResult();
                }
        
                if (!TryParseStatusFilter(status, out var statuses))
                {
                    return Result.Failure<MyBookingsPage>(
                            new Error("TourBooking.InvalidStatusFilter", "One or more status values are invalid."),
                            Outcome.Invalid)
                        .ToApiResult();
                }
        
                if (!TryParseDate(fromDate, out var parsedFromDate))
                {
                    return Result.Failure<MyBookingsPage>(
                            new Error("TourBooking.InvalidDateRange", "fromDate must be in YYYY-MM-DD format."),
                            Outcome.Invalid)
                        .ToApiResult();
                }
        
                if (!TryParseDate(toDate, out var parsedToDate))
                {
                    return Result.Failure<MyBookingsPage>(
                            new Error("TourBooking.InvalidDateRange", "toDate must be in YYYY-MM-DD format."),
                            Outcome.Invalid)
                        .ToApiResult();
                }
        
                var query = new GetMyBookingsQuery(
                    currentUser.UserId.Value,
                    statuses,
                    parsedFromDate,
                    parsedToDate,
                    tourId,
                    cursor,
                    pageSize,
                    countTotal);
        
                var result = await sender.Send(query, cancellationToken);
                return result.ToApiResult();
            })
            .WithName("GetMyBookings")
            .WithSummary("List the caller's tour bookings (cursor paginated).")
            .WithDescription(
                "Returns bookings owned by the authenticated user, ordered by CreatedAt DESC. " +
                "Supports filters by status (comma-separated), slot-date range, and tourId. " +
                "Pagination via opaque cursor (B-R10). Page size clamps to [1, 50] (default 20).")
            .Produces<MyBookingsPage>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.TourBooking, AppAction.ReadOwn))
            .RequireAuthorization();
    }

    private static void MapGetProviderBookingsEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/provider/bookings", async (
                ICurrentUser currentUser,
                ISender sender,
                CancellationToken cancellationToken,
                string? status = null,
                string? fromDate = null,
                string? toDate = null,
                Guid? tourId = null,
                string? cursor = null,
                int pageSize = GetProviderBookingsQuery.DefaultPageSize,
                bool countTotal = false) =>
            {
                if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                {
                    return Result.Failure<ProviderBookingsPage>(
                            new Error("TourBooking.Unauthorized", "Authentication is required."),
                            Outcome.Unauthorized)
                        .ToApiResult();
                }

                if (!TryParseStatusFilter(status, out var statuses))
                {
                    return Result.Failure<ProviderBookingsPage>(
                            new Error("TourBooking.InvalidStatusFilter", "One or more status values are invalid."),
                            Outcome.Invalid)
                        .ToApiResult();
                }

                if (!TryParseDate(fromDate, out var parsedFromDate))
                {
                    return Result.Failure<ProviderBookingsPage>(
                            new Error("TourBooking.InvalidDateRange", "fromDate must be in YYYY-MM-DD format."),
                            Outcome.Invalid)
                        .ToApiResult();
                }

                if (!TryParseDate(toDate, out var parsedToDate))
                {
                    return Result.Failure<ProviderBookingsPage>(
                            new Error("TourBooking.InvalidDateRange", "toDate must be in YYYY-MM-DD format."),
                            Outcome.Invalid)
                        .ToApiResult();
                }

                var query = new GetProviderBookingsQuery(
                    currentUser.UserId.Value,
                    statuses,
                    parsedFromDate,
                    parsedToDate,
                    tourId,
                    cursor,
                    pageSize,
                    countTotal);

                var result = await sender.Send(query, cancellationToken);
                return result.ToApiResult();
            })
            .WithName("GetProviderBookings")
            .WithSummary("Provider: list bookings for tours owned by the caller's provider (cursor paginated).")
            .WithDescription(
                "Owner-scoped. Resolves the caller's provider via ProviderSnapshot.OwnerUserId and returns " +
                "ONLY bookings for that provider (empty page if the caller owns no provider). " +
                "Supports status (comma-separated), slot-date range, and tourId filters. " +
                "Each row includes the slot date/time. Requires TourBooking.ReadOwn.")
            .Produces<ProviderBookingsPage>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.TourBooking, AppAction.ReadOwn))
            .RequireAuthorization();
    }

    private static void MapGetAllBookingsEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/admin/all", async (
                ISender sender,
                CancellationToken cancellationToken,
                string? status = null,
                string? fromDate = null,
                string? toDate = null,
                Guid? userId = null,
                Guid? providerId = null,
                Guid? tourId = null,
                string? paymentStatus = null,
                string? cursor = null,
                int pageSize = GetAllBookingsQuery.DefaultPageSize,
                bool countTotal = false) =>
            {
                if (!TryParseStatusFilter(status, out var statuses))
                {
                    return Result.Failure<AdminBookingsPage>(
                            new Error("TourBooking.InvalidStatusFilter", "One or more status values are invalid."),
                            Outcome.Invalid)
                        .ToApiResult();
                }
        
                if (!TryParseDate(fromDate, out var parsedFromDate))
                {
                    return Result.Failure<AdminBookingsPage>(
                            new Error("TourBooking.InvalidDateRange", "fromDate must be in YYYY-MM-DD format."),
                            Outcome.Invalid)
                        .ToApiResult();
                }
        
                if (!TryParseDate(toDate, out var parsedToDate))
                {
                    return Result.Failure<AdminBookingsPage>(
                            new Error("TourBooking.InvalidDateRange", "toDate must be in YYYY-MM-DD format."),
                            Outcome.Invalid)
                        .ToApiResult();
                }
        
                var query = new GetAllBookingsQuery(
                    statuses,
                    parsedFromDate,
                    parsedToDate,
                    userId,
                    providerId,
                    tourId,
                    paymentStatus,
                    cursor,
                    pageSize,
                    countTotal);
        
                var result = await sender.Send(query, cancellationToken);
                return result.ToApiResult();
            })
            .WithName("GetAllBookings")
            .WithSummary("Admin: list ALL bookings across all users and providers.")
            .WithDescription(
                "Admin dashboard endpoint. Returns every booking matching the optional filters " +
                "(status, slot-date range, userId, providerId, tourId, paymentStatus). " +
                "Cursor paginated, same shape as /my-bookings (B-R10). " +
                "Requires the AdminBookingDashboard.Read permission. " +
                "paymentStatus is accepted as a hint but is currently a no-op until " +
                "the Finance cross-module payment projection ships.")
            .Produces<AdminBookingsPage>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.AdminBookingDashboard, AppAction.Read))
            .RequireAuthorization();
    }

    private static bool TryParseStatusFilter(string? raw, out IReadOnlyList<BookingStatus>? statuses)
    {
        statuses = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return true;
        }

        var parsed = new List<BookingStatus>(capacity: parts.Length);
        foreach (var part in parts)
        {
            if (!Enum.TryParse<BookingStatus>(part, ignoreCase: true, out var value)
                || !Enum.IsDefined(value))
            {
                return false;
            }

            parsed.Add(value);
        }

        statuses = parsed;
        return true;
    }

    private static bool TryParseDate(string? raw, out DateOnly? date)
    {
        date = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        if (!DateOnly.TryParseExact(
            raw,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed))
        {
            return false;
        }

        date = parsed;
        return true;
    }
}
