using MediatR;
using Messaging.Application.Commands.CreateNotificationTemplate;
using Messaging.Application.Commands.DeleteNotificationTemplate;
using Messaging.Application.Commands.UpdateNotificationTemplate;
using Messaging.Application.Queries.GetNotificationTemplates;
using Messaging.Contracts.Authorization;
using Messaging.Presentation.Endpoints.NotificationTemplate.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Messaging.Presentation.Endpoints.NotificationTemplate;

internal static class NotificationTemplateEndpoints
{
    internal static void MapNotificationTemplateEndpoints(RouteGroupBuilder group)
    {
        group.WithTags("Messaging | Notification Templates");

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetNotificationTemplatesQuery(), ct);
            return result.ToApiResult();
        })
        .WithName("GetNotificationTemplates")
        .Produces<IReadOnlyList<NotificationTemplateDto>>(StatusCodes.Status200OK)
        .WithSummary("List all notification templates.")
        .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.NotificationTemplate, AppAction.Read))
        .RequireAuthorization();

        group.MapPost("/", async (
            CreateTemplateRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new CreateNotificationTemplateCommand(
                request.Type,
                request.Channel,
                request.LanguageCode,
                request.Title,
                request.Body,
                request.HtmlBody);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("CreateNotificationTemplate")
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .WithSummary("Create a notification template.")
        .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.NotificationTemplate, AppAction.Create))
        .RequireAuthorization();

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateTemplateRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new UpdateNotificationTemplateCommand(id, request.Title, request.Body, request.HtmlBody);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("UpdateNotificationTemplate")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Update a notification template.")
        .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.NotificationTemplate, AppAction.Update))
        .RequireAuthorization();

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteNotificationTemplateCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteNotificationTemplate")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Delete a notification template.")
        .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.NotificationTemplate, AppAction.Delete))
        .RequireAuthorization();
    }
}
