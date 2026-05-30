using MediatR;
using Messaging.Application.Commands.BatchDeleteNotifications;
using Messaging.Application.Commands.DeleteNotification;
using Messaging.Application.Commands.MarkAllNotificationsRead;
using Messaging.Application.Commands.MarkNotificationRead;
using Messaging.Application.Commands.UpdatePreferences;
using Messaging.Application.Queries.GetMyNotifications;
using Messaging.Application.Queries.GetMyPreferences;
using Messaging.Application.Queries.GetNotificationById;
using Messaging.Application.Queries.GetUnreadCount;
using Messaging.Contracts.Authorization;
using Messaging.Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Messaging.Presentation.Endpoints;

internal static class NotificationEndpoints
{
    internal static RouteGroupBuilder MapNotificationEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            [AsParameters] GetNotificationsRequest req,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetMyNotificationsQuery(
                currentUser.UserId!.Value,
                req.Type,
                req.IsRead,
                req.From,
                req.To,
                req.Cursor,
                req.PageSize ?? 20);
            var result = await sender.Send(query, ct);
            return result.ToApiResult();
        }).WithName("GetMyNotifications").WithTags("Notifications")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.Notification, AppAction.Read))
          .RequireAuthorization();

        group.MapGet("/unread-count", async (
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetUnreadCountQuery(currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        }).WithName("GetUnreadNotificationCount").WithTags("Notifications")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.Notification, AppAction.Read))
          .RequireAuthorization();

        group.MapGet("/preferences", async (
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyPreferencesQuery(currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        }).WithName("GetMyNotificationPreferences").WithTags("Notifications")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.NotificationPreference, AppAction.Read))
          .RequireAuthorization();

        group.MapPut("/preferences", async (
            UpdatePreferencesRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new UpdatePreferencesCommand(currentUser.UserId!.Value, request.Updates);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        }).WithName("UpdateNotificationPreferences").WithTags("Notifications")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.NotificationPreference, AppAction.Update))
          .RequireAuthorization();

        group.MapPost("/{id:guid}/read", async (
            Guid id,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new MarkNotificationReadCommand(id, currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        }).WithName("MarkNotificationRead").WithTags("Notifications")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.Notification, AppAction.Update))
          .RequireAuthorization();

        group.MapPost("/read-all", async (
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new MarkAllNotificationsReadCommand(currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        }).WithName("MarkAllNotificationsRead").WithTags("Notifications")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.Notification, AppAction.Update))
          .RequireAuthorization();

        group.MapDelete("/batch", async (
            [FromBody] BatchDeleteNotificationsRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new BatchDeleteNotificationsCommand(request.Ids, currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        }).WithName("BatchDeleteNotifications").WithTags("Notifications")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.Notification, AppAction.Delete))
          .RequireAuthorization();

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteNotificationCommand(id, currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        }).WithName("DeleteNotification").WithTags("Notifications")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.Notification, AppAction.Delete))
          .RequireAuthorization();

        group.MapGet("/{id:guid}", async (
            Guid id,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetNotificationByIdQuery(id, currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        }).WithName("GetNotificationById").WithTags("Notifications")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.Notification, AppAction.Read))
          .RequireAuthorization();

        return group;
    }
}

internal sealed record GetNotificationsRequest(
    NotificationType? Type,
    bool? IsRead,
    DateTime? From,
    DateTime? To,
    Guid? Cursor,
    int? PageSize);

internal sealed record UpdatePreferencesRequest(IReadOnlyList<PreferenceUpdate> Updates);

internal sealed record BatchDeleteNotificationsRequest(IReadOnlyList<Guid> Ids);
