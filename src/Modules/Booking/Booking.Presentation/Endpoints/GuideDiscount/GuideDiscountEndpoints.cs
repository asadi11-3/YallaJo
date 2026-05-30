using Booking.Application.Commands.CreateGuideDiscount;
using Booking.Application.Commands.DeactivateGuideDiscount;
using Booking.Application.Commands.UpdateGuideDiscount;
using Booking.Application.Queries.GetMyGuideDiscounts;
using Booking.Contracts.Authorization;
using Booking.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Booking.Presentation.Endpoints.GuideDiscount;

/// <summary>
/// Guide discount endpoints mounted under <c>/api/v1/booking/guide-discounts</c>.
/// </summary>
internal static class GuideDiscountEndpoints
{
    internal static void MapGuideDiscountEndpoints(RouteGroupBuilder group)
    {
        // GET /guide-discounts/mine — list own discounts
        group.MapGet("/mine", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetMyGuideDiscountsQuery(), ct);
                return result.ToApiResult();
            })
            .WithName("GetMyGuideDiscounts")
            .WithSummary("Guide lists their own discounts.")
            .WithTags("Booking | GuideDiscount")
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.TourBooking, AppAction.ReadOwn))
            .RequireAuthorization();

        // POST /guide-discounts — create
        group.MapPost("/", async (
                CreateGuideDiscountRequest request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(request.ToCommand(), ct);
                return result.ToApiResult();
            })
            .WithName("CreateGuideDiscount")
            .WithSummary("Guide creates a discount for their tour offerings.")
            .WithTags("Booking | GuideDiscount")
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.TourBooking, AppAction.Create))
            .RequireAuthorization();

        // PUT /guide-discounts/{id} — update
        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateGuideDiscountRequest request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(request.ToCommand(id), ct);
                return result.ToApiResult();
            })
            .WithName("UpdateGuideDiscount")
            .WithSummary("Guide updates an existing discount.")
            .WithTags("Booking | GuideDiscount")
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.TourBooking, AppAction.Update))
            .RequireAuthorization();

        // DELETE /guide-discounts/{id} — deactivate
        group.MapDelete("/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new DeactivateGuideDiscountCommand(id), ct);
                return result.ToApiResult();
            })
            .WithName("DeactivateGuideDiscount")
            .WithSummary("Guide deactivates a discount.")
            .WithTags("Booking | GuideDiscount")
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.TourBooking, AppAction.Delete))
            .RequireAuthorization();
    }
}

public sealed record CreateGuideDiscountRequest(
    Guid? TourId,
    string Name,
    string? Description,
    GuideDiscountType DiscountType,
    decimal DiscountValue,
    string Currency,
    DateTime ValidFrom,
    DateTime? ValidUntil,
    int? MaxUsageCount)
{
    public CreateGuideDiscountCommand ToCommand()
        => new(TourId, Name, Description, DiscountType, DiscountValue, Currency, ValidFrom, ValidUntil, MaxUsageCount);
}

public sealed record UpdateGuideDiscountRequest(
    string Name,
    string? Description,
    decimal DiscountValue,
    DateTime ValidFrom,
    DateTime? ValidUntil,
    int? MaxUsageCount)
{
    public UpdateGuideDiscountCommand ToCommand(Guid id)
        => new(id, Name, Description, DiscountValue, ValidFrom, ValidUntil, MaxUsageCount);
}
