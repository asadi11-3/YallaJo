using MediatR;
using Messaging.Application.Commands.CreateNotificationTemplate;
using Messaging.Application.Commands.DeleteNotificationTemplate;
using Messaging.Application.Commands.UpdateNotificationTemplate;
using Messaging.Application.Queries.GetNotificationTemplates;
using Messaging.Contracts.Authorization;
using Messaging.Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Messaging.Presentation.Endpoints;

internal static class NotificationTemplateEndpoints
{
    internal static RouteGroupBuilder MapNotificationTemplateEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetNotificationTemplatesQuery(), ct);
            return result.ToApiResult();
        }).WithName("GetNotificationTemplates").WithTags("NotificationTemplates")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.NotificationTemplate, AppAction.Read))
          .RequireAuthorization();

        group.MapPost("/", async (
            CreateTemplateRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return Result.Failure<Guid>(new Error("NotificationTemplate.Unauthorized", "Not authenticated"), Outcome.Unauthorized).ToApiResult();
            var cmd = new CreateNotificationTemplateCommand(
                request.Type,
                request.Channel,
                request.LanguageCode,
                request.Title,
                request.Body,
                request.HtmlBody);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        }).WithName("CreateNotificationTemplate").WithTags("NotificationTemplates")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.NotificationTemplate, AppAction.Create))
          .RequireAuthorization();

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateTemplateRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return Result.Failure(new Error("NotificationTemplate.Unauthorized", "Not authenticated"), Outcome.Unauthorized).ToApiResult();
            var cmd = new UpdateNotificationTemplateCommand(id, request.Title, request.Body, request.HtmlBody);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        }).WithName("UpdateNotificationTemplate").WithTags("NotificationTemplates")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.NotificationTemplate, AppAction.Update))
          .RequireAuthorization();

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return Result.Failure(new Error("NotificationTemplate.Unauthorized", "Not authenticated"), Outcome.Unauthorized).ToApiResult();
            var result = await sender.Send(new DeleteNotificationTemplateCommand(id), ct);
            return result.ToApiResult();
        }).WithName("DeleteNotificationTemplate").WithTags("NotificationTemplates")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.NotificationTemplate, AppAction.Delete))
          .RequireAuthorization();

        return group;
    }
}

internal sealed record CreateTemplateRequest(NotificationType Type, NotificationChannel Channel, string LanguageCode, string Title, string Body, string? HtmlBody);
internal sealed record UpdateTemplateRequest(string Title, string Body, string? HtmlBody);
