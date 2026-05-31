using MediatR;
using Messaging.Application.Commands.BatchDeleteNotifications;
using Messaging.Application.Commands.DeleteNotification;
using Messaging.Application.Commands.MarkAllNotificationsRead;
using Messaging.Application.Commands.MarkNotificationRead;
using Messaging.Application.Commands.UpdatePreferences;
using Messaging.Application.Queries.Dtos;
using Messaging.Application.Queries.GetMyNotifications;
using Messaging.Application.Queries.GetMyPreferences;
using Messaging.Application.Queries.GetNotificationById;
using Messaging.Application.Queries.GetUnreadCount;
using Messaging.Contracts.Authorization;
using Messaging.Presentation.Endpoints.Notification.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Messaging.Presentation.Endpoints.Notification;

internal static class NotificationEndpoints
{
    internal static void MapNotificationEndpoints(RouteGroupBuilder group)
    {
        group.WithTags("Messaging | Notifications");

        group.MapGet("/", async (
            [AsParameters] GetNotificationsRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetMyNotificationsQuery(
                currentUser.UserId!.Value,
                request.Type,
                request.IsRead,
                request.From,
                request.To,
                request.Cursor,
                request.PageSize ?? 20);
            var result = await sender.Send(query, ct);
            return result.ToApiResult();
        }).WithName("GetMyNotifications")
          .Produces<NotificationPageDto>(StatusCodes.Status200OK)
          .WithSummary("Get the current user's notifications (paged).")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.Notification, AppAction.Read))
          .RequireAuthorization();

        group.MapGet("/unread-count", async (
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetUnreadCountQuery(currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        }).WithName("GetUnreadNotificationCount")
          .Produces<int>(StatusCodes.Status200OK)
          .WithSummary("Get the count of the current user's unread notifications.")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.Notification, AppAction.Read))
          .RequireAuthorization();

        group.MapGet("/preferences", async (
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyPreferencesQuery(currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        }).WithName("GetMyNotificationPreferences")
          .Produces<IReadOnlyList<NotificationPreferenceDto>>(StatusCodes.Status200OK)
          .WithSummary("Get the current user's notification preferences.")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.NotificationPreference, AppAction.Read))
          .RequireAuthorization();

        group.MapPut("/preferences", async (
            UpdatePreferencesRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdatePreferencesCommand(currentUser.UserId!.Value, request.Updates), ct);
            return result.ToApiResult();
        }).WithName("UpdateNotificationPreferences")
          .Produces(StatusCodes.Status200OK)
          .ProducesValidationProblem()
          .WithSummary("Update the current user's notification preferences.")
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
        }).WithName("MarkNotificationRead")
          .Produces(StatusCodes.Status200OK)
          .ProducesProblem(StatusCodes.Status404NotFound)
          .WithSummary("Mark a notification as read.")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.Notification, AppAction.Update))
          .RequireAuthorization();

        group.MapPost("/read-all", async (
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new MarkAllNotificationsReadCommand(currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        }).WithName("MarkAllNotificationsRead")
          .Produces(StatusCodes.Status200OK)
          .WithSummary("Mark all of the current user's notifications as read.")
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
        }).WithName("BatchDeleteNotifications")
          .Produces(StatusCodes.Status200OK)
          .ProducesValidationProblem()
          .WithSummary("Delete multiple notifications for the current user.")
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
        }).WithName("DeleteNotification")
          .Produces(StatusCodes.Status200OK)
          .ProducesProblem(StatusCodes.Status404NotFound)
          .WithSummary("Delete a notification for the current user.")
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
        }).WithName("GetNotificationById")
          .Produces<NotificationDto>(StatusCodes.Status200OK)
          .ProducesProblem(StatusCodes.Status404NotFound)
          .WithSummary("Get a notification by id.")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.Notification, AppAction.Read))
          .RequireAuthorization();
    }
}
