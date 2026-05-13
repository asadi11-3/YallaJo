using ContentTours.Application.Commands.TourPricingTier.CreateTourPricingTier;
using ContentTours.Application.Commands.TourPricingTier.DeleteTourPricingTier;
using ContentTours.Application.Commands.TourPricingTier.UpdateTourPricingTier;
using ContentTours.Application.Queries.TourPricingTier.Common;
using ContentTours.Application.Queries.TourPricingTier.ListTourPricingTiers;
using ContentTours.Contracts.Authorization;
using ContentTours.Presentation.Endpoints.TourPricingTier.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace ContentTours.Presentation.Endpoints.TourPricingTier;

internal static class TourPricingTierEndpoints
{
    internal static void MapTourPricingTierEndpoints(RouteGroupBuilder group)
    {
        var pricing = group.MapGroup("/{id}/pricing").WithTags("ContentTours | Pricing");

        pricing.MapGet("/", async (
            Guid id,
            bool? activeOnly,
            string? lang,
            HttpContext http,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var languageCode = lang ?? http.Request.Headers.AcceptLanguage.ToString();
            var callerUserId = currentUser.IsAuthenticated ? currentUser.UserId : null;
            var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
                >= RolePrivilegeLevel.Admin;

            var result = await sender.Send(
                new ListTourPricingTiersQuery(
                    TourId: id,
                    ActiveOnly: activeOnly ?? true,
                    LanguageCode: languageCode,
                    CallerUserId: callerUserId,
                    IsAdmin: isAdminTier),
                ct);
            return result.ToApiResult();
        })
        .WithName("ListTourPricingTiers")
        .WithSummary("List pricing tiers for a tour")
        .Produces<IReadOnlyList<TourPricingTierDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();

        pricing.MapPost("/", async (
            Guid id,
            CreateTourPricingTierRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new CreateTourPricingTierCommand(
                id, request.Name, request.Description,
                request.Price, request.Currency,
                request.ParticipantType,
                request.MinParticipants, request.MaxParticipants);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("CreateTourPricingTier")
        .WithSummary("Create a pricing tier for a tour")
        .Produces<CreateTourPricingTierResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourPricingTier, AppAction.Create));

        pricing.MapPut("/{tierId:guid}", async (
            Guid id,
            Guid tierId,
            UpdateTourPricingTierRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new UpdateTourPricingTierCommand(
                id, tierId,
                request.Name, request.Description,
                request.Price, request.Currency,
                request.ParticipantType,
                request.MinParticipants, request.MaxParticipants,
                request.IsActive);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("UpdateTourPricingTier")
        .WithSummary("Update a pricing tier")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourPricingTier, AppAction.Update));

        pricing.MapDelete("/{tierId:guid}", async (
            Guid id,
            Guid tierId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteTourPricingTierCommand(id, tierId), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteTourPricingTier")
        .WithSummary("Delete a pricing tier (Adult-tier guard applies)")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourPricingTier, AppAction.Delete));
    }

}
