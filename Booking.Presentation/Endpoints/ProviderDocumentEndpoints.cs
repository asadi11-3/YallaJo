using System;
using Booking.Application.Commands.UploadProviderDocument;
using Booking.Application.Commands.UpdateProviderDocument;
using Booking.Application.Queries.GetProviderDocument;
using Booking.Application.Queries.GetProviderDocuments;
using Booking.Contracts.Authorization;
using Booking.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Booking.Presentation.Endpoints;

public static class ProviderDocumentEndpoints
{
    public static void MapProviderDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/booking/provider/documents")
                       .WithTags("Provider Documents");

        [MustHavePermission(BookingFeatures.ProviderDocument, AppAction.Create)]
        group.MapPost("/", async (
            [FromForm] DocumentType documentType,
            [FromForm] IFormFile file,
            [FromForm] DateTime? expiresAt,
            ISender sender) =>
        {
            var command = new UploadProviderDocumentCommand(documentType, file, expiresAt);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Created($"/api/v1/booking/provider/documents/{result.Value}", result.Value)
                : Results.BadRequest(result.Error);
        })
        .DisableAntiforgery();

        [MustHavePermission(BookingFeatures.ProviderDocument, AppAction.Update)]
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromForm] IFormFile? file,
            [FromForm] DateTime? expiresAt,
            ISender sender) =>
        {
            var command = new UpdateProviderDocumentCommand(id, file, expiresAt);
            var result = await sender.Send(command);

            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.IsFailure);
        })
        .DisableAntiforgery();

        group.MapGet("/{providerId:guid}/list", async (
            Guid providerId,
            ISender sender) =>
        {
            var result = await sender.Send(new GetProviderDocumentsQuery(providerId));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender) =>
        {
            var result = await sender.Send(new GetProviderDocumentQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        });
    }
}
