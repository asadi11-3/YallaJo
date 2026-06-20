using ContentTours.Application.Commands.TourPackage.AddInclusion;
using ContentTours.Application.Commands.TourPackage.ApproveTourPackage;
using ContentTours.Application.Commands.TourPackage.CreateTourPackage;
using ContentTours.Application.Commands.TourPackage.DeleteTourPackage;
using ContentTours.Application.Commands.TourPackage.RejectTourPackage;
using ContentTours.Application.Commands.TourPackage.SubmitTourPackage;
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
            .WithTags("ContentTours | TourPackages")
            .RequireAuthorization();

        packages.MapGet("/", async (
            int? page,
            int? pageSize,
            Guid? providerId,
            decimal? minPrice,
            decimal? maxPrice,
            string? currency,
            Guid? includeTourId,
            DateTime? validOnDate,
            string? sort,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new ListTourPackagesQuery(
                    Page:          page ?? 1,
                    PageSize:      pageSize ?? 20,
                    ProviderId:    providerId,
                    MinPrice:      minPrice,
                    MaxPrice:      maxPrice,
                    Currency:      currency,
                    IncludeTourId: includeTourId,
                    ValidOnDate:   validOnDate,
                    Sort:          sort ?? "newest"),
                ct);

            return result.ToApiResult();
        })
        .WithName("ListTourPackages")
        .WithSummary("List tour packages with pagination")
        .Produces<PaginatedResult<TourPackageSummaryDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .AllowAnonymous();

        // ── GET /packages/{id} ────────────────────────────────────────
        packages.MapGet("/{id:guid}", async (
            Guid id,
            HttpRequest httpRequest,
            ISender sender,
            CancellationToken ct) =>
        {
            var acceptLanguage = httpRequest.Headers.AcceptLanguage.ToString();
            var result = await sender.Send(new GetTourPackageByIdQuery(id, acceptLanguage), ct);
            return result.ToApiResult();
        })
        .WithName("GetTourPackageById")
        .WithSummary("Get tour package by id")
        .Produces<TourPackageDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();

        // ── POST /packages ────────────────────────────────────────────
        packages.MapPost("/", async (
            CreateTourPackageRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new CreateTourPackageCommand(
                Name:            request.Name?.Trim() ?? string.Empty,
                Description:     request.Description?.Trim(),
                Price:           request.Price,
                Currency:        request.Currency?.Trim().ToUpperInvariant() ?? string.Empty,
                MaxParticipants: request.MaxParticipants,
                ValidFrom:       request.ValidFrom,
                ValidTo:         request.ValidTo,
                IncludedTourIds: request.IncludedTourIds ?? Array.Empty<Guid>(),
                Inclusions:      request.Inclusions ?? Array.Empty<string>());

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
            ContentToursFeatures.Package,
            AppAction.Create));

        // ── PUT /packages/{id} ────────────────────────────────────────
        packages.MapPut("/{id:guid}", async (
            Guid id,
            UpdateTourPackageRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            byte[] rowVersion;
            try
            {
                rowVersion = string.IsNullOrWhiteSpace(request.RowVersion)
                    ? Array.Empty<byte>()
                    : Convert.FromBase64String(request.RowVersion);
            }
            catch (FormatException)
            {
                rowVersion = Array.Empty<byte>();
            }

            var cmd = new UpdateTourPackageCommand(
                Id:              id,
                Name:            request.Name,
                Description:     request.Description,
                Price:           request.Price,
                Currency:        request.Currency,
                MaxParticipants: request.MaxParticipants,
                ValidFrom:       request.ValidFrom,
                ValidTo:         request.ValidTo,
                IncludedTourIds: request.IncludedTourIds ?? Array.Empty<Guid>(),
                RowVersion:      rowVersion);

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
            ContentToursFeatures.Package,
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
        .WithSummary("Soft-delete a tour package")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(
            ContentToursFeatures.Package,
            AppAction.Delete));

        // ── POST /packages/{id}/inclusions ────────────────────────────
        packages.MapPost("/{id:guid}/inclusions", async (
            Guid id,
            AddInclusionRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new AddInclusionCommand(
                PackageId:   id,
                Description: request.Description);

            var result = await sender.Send(cmd, ct);

            return result.ToApiResult();
        })
        .WithName("AddTourPackageInclusion")
        .WithSummary("Add inclusion to tour package")
        .Produces<Guid>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(
            ContentToursFeatures.Package,
            AppAction.Update));

        // ── POST /packages/{id}/submit ─ WS-5a (Phase 3 G3a) ──────────
        packages.MapPost("/{id:guid}/submit", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new SubmitTourPackageCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("SubmitTourPackage")
        .WithSummary("Submit a tour package for admin review (provider)")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(
            ContentToursFeatures.Package,
            AppAction.Submit))
        .RequireAuthorization();

        // ── POST /packages/{id}/approve ─ WS-5a (Phase 3 G3a) ─────────
        packages.MapPost("/{id:guid}/approve", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ApproveTourPackageCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("ApproveTourPackage")
        .WithSummary("Approve a tour package (Admin)")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(
            ContentToursFeatures.Package,
            AppAction.Approve))
        .RequireAuthorization();

        // ── POST /packages/{id}/reject ─ WS-5a (Phase 3 G3a) ──────────
        packages.MapPost("/{id:guid}/reject", async (
            Guid id,
            RejectTourPackageRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new RejectTourPackageCommand(id, request.Reason),
                ct);
            return result.ToApiResult();
        })
        .WithName("RejectTourPackage")
        .WithSummary("Reject a tour package (Admin)")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(
            ContentToursFeatures.Package,
            AppAction.Reject))
        .RequireAuthorization();
    }
}
