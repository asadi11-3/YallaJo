using System.Text.Json;

using Booking.Application.Interfaces;
using Booking.Domain.Repositories;
using Booking.Domain.ValueObjects;

using Microsoft.Extensions.Logging;

using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Queries.GetTourBookingById;

/// <summary>
/// Loads a single tour booking and projects it to <see cref="TourBookingDetailDto"/>.
/// Enforces IDOR: only the owning user, the tour's provider, or an admin may read.
/// </summary>
public sealed class GetTourBookingByIdQueryHandler(
    ITourBookingRepository repository,
    IBookingTourSnapshotReader tourSnapshotReader,
    IBookingProviderSnapshotReader providerSnapshotReader,
    ILogger<GetTourBookingByIdQueryHandler> logger)
    : IQueryHandler<GetTourBookingByIdQuery, TourBookingDetailDto>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<TourBookingDetailDto>> Handle(
        GetTourBookingByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var booking = await repository
                .GetByIdWithDetailsAsync(request.BookingId, cancellationToken)
                .ConfigureAwait(false);

            if (booking is null)
            {
                logger.LogInformation("Booking {BookingId} not found", request.BookingId);
                return Result.Failure<TourBookingDetailDto>(
                    new Error("TourBooking.NotFound", "Booking was not found."),
                    Outcome.NotFound);
            }

            // IDOR guard. Owner is the cheapest check and the most common path.
            var viewerId = request.ViewerUserId;
            var isOwner = booking.UserId == viewerId;
            var isAdmin = request.ViewerIsAdmin;
            var isProvider = false;

            if (!isOwner && !isAdmin)
            {
                // Provider check requires resolving the tour's provider and comparing
                // its OwnerUserId. We pull the provider snapshot rather than the tour
                // because booking.ProviderId is already known.
                var provider = await providerSnapshotReader
                    .GetByIdAsync(booking.ProviderId, cancellationToken)
                    .ConfigureAwait(false);

                isProvider = provider is not null && provider.OwnerUserId == viewerId;

                if (!isProvider)
                {
                    logger.LogWarning(
                        "Viewer {ViewerId} attempted to read booking {BookingId} they do not own",
                        viewerId,
                        booking.Id);

                    return Result.Failure<TourBookingDetailDto>(
                        new Error("TourBooking.OwnerMismatch", "You are not authorized to view this booking."),
                        Outcome.Forbidden);
                }
            }

            // Optional: hydrate tour-snapshot data into the DTO in a future iteration
            // (title, slot date/time). For now we keep the projection self-contained
            // using only booking-side data to avoid an extra round-trip on cache miss.
            _ = tourSnapshotReader;

            var lineItems = DeserializeLineItems(booking.LineItemsJson);

            var pricingDto = new TourBookingPricingDto(
                booking.Subtotal,
                booking.DiscountAmount,
                booking.LoyaltyAmount,
                booking.TotalAmount,
                booking.Currency,
                lineItems);

            TourBookingConfirmationDto? confirmationDto = booking.ConfirmedAt.HasValue
                ? new TourBookingConfirmationDto(booking.ConfirmedAt.Value, booking.ConfirmationSource ?? Booking.Domain.Enums.ConfirmationSource.Manual)
                : null;

            TourBookingRejectionDto? rejectionDto = booking.RejectedAt.HasValue && !string.IsNullOrEmpty(booking.RejectionReason)
                ? new TourBookingRejectionDto(booking.RejectedAt.Value, booking.RejectionReason!)
                : null;

            TourBookingCancellationDto? cancellationDto = booking.CancelledAt.HasValue
                ? new TourBookingCancellationDto(
                    booking.CancelledAt.Value,
                    booking.CancellationSource ?? Booking.Domain.Enums.CancellationSource.User,
                    booking.CancellationReason,
                    booking.RefundAmount)
                : null;

            TourBookingCompletionDto? completionDto = booking.CompletedAt.HasValue
                ? new TourBookingCompletionDto(booking.CompletedAt.Value, booking.CompletedByUserId)
                : null;

            // FE-1A: surface the dispute lifecycle so the booking detail page can render
            // the open-dispute reason and (once resolved) the admin resolution notes.
            TourBookingDisputeDto? disputeDto = booking.DisputedAt.HasValue
                ? new TourBookingDisputeDto(
                    booking.DisputedAt.Value,
                    booking.DisputeReason ?? string.Empty,
                    booking.DisputeOpenedByUserId,
                    booking.ResolvedAt,
                    booking.ResolutionNotes,
                    booking.ResolvedByAdminId)
                : null;

            var dto = new TourBookingDetailDto(
                booking.Id,
                booking.Reference,
                booking.Status,
                booking.UserId,
                booking.TourId,
                booking.ProviderId,
                booking.AvailabilitySlotId,
                booking.ParticipantCount,
                pricingDto,
                booking.IsInstantBooking,
                booking.PaymentExpiresAt,
                booking.SpecialRequests,
                confirmationDto,
                rejectionDto,
                cancellationDto,
                completionDto,
                disputeDto,
                booking.CreatedAt,
                booking.UpdatedAt);

            logger.LogDebug(
                "Booking {BookingId} read by viewer {ViewerId} (isOwner={IsOwner}, isProvider={IsProvider}, isAdmin={IsAdmin})",
                booking.Id,
                viewerId,
                isOwner,
                isProvider,
                isAdmin);

            return Result.Success(dto);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<TourBookingDetailDto>(
                new Error("Request.Cancelled", "The request was cancelled by the client."),
                Outcome.Canceled);
        }
    }

    private static IReadOnlyList<TourBookingLineItemDto> DeserializeLineItems(string lineItemsJson)
    {
        if (string.IsNullOrWhiteSpace(lineItemsJson))
        {
            return [];
        }

        try
        {
            var raw = JsonSerializer.Deserialize<List<BookingLineItem>>(lineItemsJson, JsonOptions);
            if (raw is null)
            {
                return [];
            }

            var result = new List<TourBookingLineItemDto>(raw.Count);
            foreach (var item in raw)
            {
                result.Add(new TourBookingLineItemDto(item.TierType, item.Count, item.UnitPrice));
            }

            return result;
        }
        catch (JsonException)
        {
            // Defensive: a malformed snapshot should not 500 the read endpoint.
            // The booking still exists; we just expose an empty line-item array.
            return [];
        }
    }
}
