using Booking.Application.Commands.Common;
using Booking.Application.Queries.GetProviderDocumentById;
using Booking.Application.Queries.GetProviderDocuments;
using Booking.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Booking.Presentation.Endpoints.ProviderDocument;

internal static class ProviderDocumentEndpoints
{
    internal static void MapProviderDocumentEndpoints(RouteGroupBuilder group)
    {
        var docs = group.MapGroup("/provider/documents").WithTags("Booking | Provider Documents");

        MapUploadEndpoint(docs);
        MapUpdateEndpoint(docs);
        MapListEndpoint(docs);
        MapGetByIdEndpoint(docs);
    }

    private static void MapUploadEndpoint(RouteGroupBuilder docs)
    {
        docs.MapPost("/", async (
                [FromForm] UploadProviderDocumentRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = request.TryBuildCommand(out var leasedStream);
                try
                {
                    if (command is null)
                    {
                        return Results.ValidationProblem(new Dictionary<string, string[]>
                        {
                            ["file"] = ["File is required."],
                        });
                    }

                    var result = await sender.Send(command, cancellationToken);
                    return result.ToApiResult();
                }
                finally
                {
                    leasedStream?.Dispose();
                }
            })
            .WithName("UploadProviderDocument")
            .WithSummary("Upload a new provider document for the authenticated tour guide.")
            .Accepts<UploadProviderDocumentRequest>("multipart/form-data")
            .Produces<ProviderDocumentDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.ProviderDocument, AppAction.Create))
            .RequireAuthorization()
            .DisableAntiforgery();
    }

    private static void MapUpdateEndpoint(RouteGroupBuilder docs)
    {
        docs.MapPut("/{id:guid}", async (
                Guid id,
                [FromForm] UpdateProviderDocumentRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = request.BuildCommand(id, out var leasedStream);
                try
                {
                    var result = await sender.Send(command, cancellationToken);
                    return result.ToApiResult();
                }
                finally
                {
                    leasedStream?.Dispose();
                }
            })
            .WithName("UpdateProviderDocument")
            .WithSummary("Replace the file and/or update the expiry of an existing provider document.")
            .Accepts<UpdateProviderDocumentRequest>("multipart/form-data")
            .Produces<ProviderDocumentDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.ProviderDocument, AppAction.Update))
            .RequireAuthorization()
            .DisableAntiforgery();
    }

    private static void MapListEndpoint(RouteGroupBuilder docs)
    {
        docs.MapGet("/", async (
                ICurrentUser currentUser,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is null)
                {
                    return Results.Unauthorized();
                }

                var result = await sender.Send(
                    new GetProviderDocumentsQuery(currentUser.UserId.Value),
                    cancellationToken);
                return result.ToApiResult();
            })
            .WithName("GetProviderDocuments")
            .WithSummary("List provider documents owned by the authenticated tour guide.")
            .Produces<IReadOnlyList<ProviderDocumentDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.ProviderDocument, AppAction.Read))
            .RequireAuthorization();
    }

    private static void MapGetByIdEndpoint(RouteGroupBuilder docs)
    {
        docs.MapGet("/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetProviderDocumentByIdQuery(id), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("GetProviderDocumentById")
            .WithSummary("Get a single provider document by id (provider self OR admin).")
            .Produces<ProviderDocumentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.ProviderDocument, AppAction.Read))
            .RequireAuthorization();
    }
}
