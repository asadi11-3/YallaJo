using ContentTours.Application.Commands.TourPackage.AddInclusion;
using ContentTours.Application.Commands.TourPackage.CreateTourPackage;
using ContentTours.Application.Commands.TourPackage.DeleteTourPackage;
using ContentTours.Application.Commands.TourPackage.UpdateTourPackage;
using ContentTours.Application.Queries.TourPackage.Common;
using ContentTours.Application.Queries.TourPackage.GetTourPackageById;
using ContentTours.Application.Queries.TourPackage.ListTourPackages;
using ContentTours.Contracts.Authorization;
using ContentTours.Presentation.Endpoints.TourPackage.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace ContentTours.Presentation.Endpoints.TourPackage;

internal static class TourPackageEndpoints
{
    internal static void MapTourPackageEndpoints(RouteGroupBuilder group)
    {
        var packages = group.MapGroup("/packages")
            .WithTags("ContentTours | TourPackages");

        // ── GET /packages ─────────────────────────────────────────────
        packages.MapGet("/", async (
            int? page,
            int? pageSize,
            bool? isActive,
            Guid? tourId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new ListTourPackagesQuery(page ?? 1, pageSize ?? 20, isActive, tourId),
                ct);

            return result.ToApiResult();
        })
        .WithName("ListTourPackages")
        .WithSummary("List tour packages with pagination")
        .Produces<PaginatedResult<TourPackageDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithMetadata(new MustHavePermissionAttribute(
            ContentToursFeatures.Tour,
            AppAction.Read));

        // ── GET /packages/{id} ────────────────────────────────────────
        packages.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTourPackageByIdQuery(id), ct);
            return result.ToApiResult();
        })
        .WithName("GetTourPackageById")
        .WithSummary("Get tour package by id")
        .Produces<TourPackageDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(
            ContentToursFeatures.Tour,
            AppAction.Read));

        // ── POST /packages ────────────────────────────────────────────
        packages.MapPost("/", async (
            CreateTourPackageRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new CreateTourPackageCommand(
                request.TourId,
                request.Name?.Trim()!,
                request.Description?.Trim(),
                request.Price,
                request.Currency?.Trim().ToUpperInvariant()!,
                request.MaxParticipants,
                request.ValidFrom,
                request.ValidTo);

            var result = await sender.Send(cmd, ct);

            return result.ToApiResult(r => $"/api/v1/tours/packages/{r}");
        })
        .WithName("CreateTourPackage")
        .WithSummary("Create a tour package")
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(
            ContentToursFeatures.Tour,
            AppAction.Update));

        // ── PUT /packages/{id} ────────────────────────────────────────
        packages.MapPut("/{id:guid}", async (
            Guid id,
            UpdateTourPackageRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new UpdateTourPackageCommand(
                id,
                request.Name,
                request.Description,
                request.Price,
                request.Currency,
                request.MaxParticipants,
                request.ValidTo);

            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("UpdateTourPackage")
        .WithSummary("Update a tour package")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(
            ContentToursFeatures.Tour,
            AppAction.Update));

        // ── DELETE /packages/{id} ─────────────────────────────────────
        packages.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteTourPackageCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteTourPackage")
        .WithSummary("Deactivate a tour package")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(
            ContentToursFeatures.Tour,
            AppAction.Update));

        // ── POST /packages/{id}/inclusions ────────────────────────────
        packages.MapPost("/{id:guid}/inclusions", async (
            Guid id,
            AddInclusionRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new AddInclusionCommand(
                id,
                request.Description,
                request.SortOrder);

            var result = await sender.Send(cmd, ct);

            return result.ToApiResult();
        })
        .WithName("AddTourPackageInclusion")
        .WithSummary("Add inclusion to tour package")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(
            ContentToursFeatures.Tour,
            AppAction.Update));

    }
}
