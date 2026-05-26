using ContentTours.Application.Commands.GuideTourOffering.DisablePrivateTour;
using ContentTours.Application.Commands.GuideTourOffering.EnablePrivateTour;
using ContentTours.Application.Commands.GuideTourOffering.PricingTier.CreateGuidePricingTier;
using ContentTours.Application.Commands.GuideTourOffering.PricingTier.DeleteGuidePricingTier;
using ContentTours.Application.Commands.GuideTourOffering.PricingTier.UpdateGuidePricingTier;
using ContentTours.Application.Commands.GuideTourOffering.ReinstateGuideOffering;
using ContentTours.Application.Commands.GuideTourOffering.RemoveGuideOffering;
using ContentTours.Application.Commands.GuideTourOffering.Schedule.CreateGuideSchedule;
using ContentTours.Application.Commands.GuideTourOffering.Schedule.DeleteGuideSchedule;
using ContentTours.Application.Commands.GuideTourOffering.Schedule.UpdateGuideSchedule;
using ContentTours.Application.Commands.GuideTourOffering.SuspendGuideOffering;
using ContentTours.Application.Queries.GuideTourOffering.GetGuideOfferingDetail;
using ContentTours.Application.Queries.GuideTourOffering.GetGuideOfferings;
using ContentTours.Application.Queries.GuideTourOffering.GetGuidePricingTiers;
using ContentTours.Application.Queries.GuideTourOffering.GetGuideSchedules;
using ContentTours.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace ContentTours.Presentation.Endpoints.TourGuide;

internal static class GuideOfferingEndpoints
{
    internal static void MapGuideOfferingEndpoints(RouteGroupBuilder group)
    {
        // ── Offerings list & detail ─────────────────────────────────────────
        group.MapGet("/{tourId:guid}/guide-offerings", async (Guid tourId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetGuideOfferingsQuery(tourId), ct);
            return result.ToApiResult();
        })
        .WithName("GetGuideOfferings")
        .WithSummary("List all guide offerings for a tour")
        .Produces<IReadOnlyList<GuideOfferingDto>>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.GuideOffering, AppAction.Read))
        .RequireAuthorization();

        group.MapGet("/{tourId:guid}/guide-offerings/{guideId:guid}", async (Guid tourId, Guid guideId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetGuideOfferingDetailQuery(tourId, guideId), ct);
            return result.ToApiResult();
        })
        .WithName("GetGuideOfferingDetail")
        .WithSummary("Get detailed offering with schedules and pricing tiers")
        .Produces<GuideOfferingDetailDto>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.GuideOffering, AppAction.Read))
        .RequireAuthorization();

        // ── Schedules ───────────────────────────────────────────────────────
        group.MapGet("/{tourId:guid}/guide-offerings/{guideId:guid}/schedules", async (Guid tourId, Guid guideId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetGuideSchedulesQuery(tourId, guideId), ct);
            return result.ToApiResult();
        })
        .WithName("GetGuideSchedules")
        .WithSummary("List schedules for a guide offering")
        .Produces<IReadOnlyList<GuideScheduleDto>>()
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.GuideOffering, AppAction.Read))
        .RequireAuthorization();

        group.MapPost("/{tourId:guid}/guide-offerings/{guideId:guid}/schedules", async (Guid tourId, Guid guideId, CreateScheduleRequest body, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateGuideScheduleCommand(tourId, guideId, body.DayOfWeek, body.StartTime, body.EndTime), ct);
            return result.ToApiResult();
        })
        .WithName("CreateGuideSchedule")
        .WithSummary("Add a schedule slot to a guide offering")
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.GuideOffering, AppAction.Create))
        .RequireAuthorization();

        group.MapPut("/{tourId:guid}/guide-offerings/{guideId:guid}/schedules/{scheduleId:guid}", async (Guid tourId, Guid guideId, Guid scheduleId, UpdateScheduleRequest body, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateGuideScheduleCommand(scheduleId, body.DayOfWeek, body.StartTime, body.EndTime), ct);
            return result.ToApiResult();
        })
        .WithName("UpdateGuideSchedule")
        .WithSummary("Update a guide schedule slot")
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.GuideOffering, AppAction.Update))
        .RequireAuthorization();

        group.MapDelete("/{tourId:guid}/guide-offerings/{guideId:guid}/schedules/{scheduleId:guid}", async (Guid tourId, Guid guideId, Guid scheduleId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteGuideScheduleCommand(scheduleId), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteGuideSchedule")
        .WithSummary("Delete a guide schedule slot")
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.GuideOffering, AppAction.Delete))
        .RequireAuthorization();

        // ── Pricing Tiers ───────────────────────────────────────────────────
        group.MapGet("/{tourId:guid}/guide-offerings/{guideId:guid}/pricing-tiers", async (Guid tourId, Guid guideId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetGuidePricingTiersQuery(tourId, guideId), ct);
            return result.ToApiResult();
        })
        .WithName("GetGuidePricingTiers")
        .WithSummary("List pricing tiers for a guide offering")
        .Produces<IReadOnlyList<GuidePricingTierDto>>()
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.GuideOffering, AppAction.Read))
        .RequireAuthorization();

        group.MapPost("/{tourId:guid}/guide-offerings/{guideId:guid}/pricing-tiers", async (Guid tourId, Guid guideId, CreatePricingTierRequest body, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateGuidePricingTierCommand(tourId, guideId, body.Name, body.Price, body.Currency, body.MinParticipants, body.MaxParticipants, body.Description), ct);
            return result.ToApiResult();
        })
        .WithName("CreateGuidePricingTier")
        .WithSummary("Add a pricing tier to a guide offering")
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.GuideOffering, AppAction.Create))
        .RequireAuthorization();

        group.MapPut("/{tourId:guid}/guide-offerings/{guideId:guid}/pricing-tiers/{tierId:guid}", async (Guid tourId, Guid guideId, Guid tierId, UpdatePricingTierRequest body, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateGuidePricingTierCommand(tierId, body.Name, body.Price, body.Currency, body.MinParticipants, body.MaxParticipants, body.Description), ct);
            return result.ToApiResult();
        })
        .WithName("UpdateGuidePricingTier")
        .WithSummary("Update a pricing tier")
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.GuideOffering, AppAction.Update))
        .RequireAuthorization();

        group.MapDelete("/{tourId:guid}/guide-offerings/{guideId:guid}/pricing-tiers/{tierId:guid}", async (Guid tourId, Guid guideId, Guid tierId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteGuidePricingTierCommand(tierId), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteGuidePricingTier")
        .WithSummary("Delete a pricing tier")
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.GuideOffering, AppAction.Delete))
        .RequireAuthorization();

        // ── Private Tour ────────────────────────────────────────────────────
        group.MapPost("/{tourId:guid}/guide-offerings/{guideId:guid}/private-tour", async (Guid tourId, Guid guideId, EnablePrivateTourRequest body, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new EnablePrivateTourCommand(tourId, guideId, body.Multiplier, body.FlatPrice), ct);
            return result.ToApiResult();
        })
        .WithName("EnablePrivateTour")
        .WithSummary("Enable private tour pricing for a guide offering")
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.GuideOffering, AppAction.Create))
        .RequireAuthorization();

        group.MapDelete("/{tourId:guid}/guide-offerings/{guideId:guid}/private-tour", async (Guid tourId, Guid guideId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DisablePrivateTourCommand(tourId, guideId), ct);
            return result.ToApiResult();
        })
        .WithName("DisablePrivateTour")
        .WithSummary("Disable private tour pricing for a guide offering")
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.GuideOffering, AppAction.Delete))
        .RequireAuthorization();

        // ── Admin: Suspend / Reinstate / Remove ─────────────────────────────
        group.MapPost("/{tourId:guid}/guide-offerings/{guideId:guid}/suspend", async (Guid tourId, Guid guideId, SuspendOfferingRequest body, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SuspendGuideOfferingCommand(tourId, guideId, body.Reason), ct);
            return result.ToApiResult();
        })
        .WithName("SuspendGuideOffering")
        .WithSummary("Suspend a guide offering")
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.GuideOffering, AppAction.Suspend))
        .RequireAuthorization();

        group.MapPost("/{tourId:guid}/guide-offerings/{guideId:guid}/reinstate", async (Guid tourId, Guid guideId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ReinstateGuideOfferingCommand(tourId, guideId), ct);
            return result.ToApiResult();
        })
        .WithName("ReinstateGuideOffering")
        .WithSummary("Reinstate a suspended guide offering")
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.GuideOffering, AppAction.Reinstate))
        .RequireAuthorization();

        group.MapDelete("/{tourId:guid}/guide-offerings/{guideId:guid}", async (Guid tourId, Guid guideId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RemoveGuideOfferingCommand(tourId, guideId), ct);
            return result.ToApiResult();
        })
        .WithName("RemoveGuideOffering")
        .WithSummary("Remove a guide offering permanently")
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.GuideOffering, AppAction.Delete))
        .RequireAuthorization();
    }
}

// ── Request Models ──────────────────────────────────────────────────────────
public sealed record CreateScheduleRequest(byte DayOfWeek, string StartTime, string? EndTime);
public sealed record UpdateScheduleRequest(byte DayOfWeek, string StartTime, string? EndTime);
public sealed record CreatePricingTierRequest(string Name, decimal Price, string Currency, int MinParticipants, int MaxParticipants, string? Description);
public sealed record UpdatePricingTierRequest(string Name, decimal Price, string Currency, int MinParticipants, int MaxParticipants, string? Description);
public sealed record EnablePrivateTourRequest(decimal? Multiplier, decimal? FlatPrice);
public sealed record SuspendOfferingRequest(string Reason);
