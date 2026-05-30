using MediatR;
using Messaging.Application.Commands.AssignSupportTicket;
using Messaging.Application.Commands.CloseSupportTicket;
using Messaging.Application.Commands.CreateSupportTicket;
using Messaging.Application.Commands.PostTicketMessage;
using Messaging.Application.Commands.ResolveSupportTicket;
using Messaging.Application.Queries.GetSupportTicketById;
using Messaging.Application.Queries.GetSupportTickets;
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

internal static class SupportTicketEndpoints
{
    internal static RouteGroupBuilder MapSupportTicketEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/tickets", async (
            CreateTicketRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new CreateSupportTicketCommand(
                currentUser.UserId!.Value,
                request.Category,
                request.Subject,
                request.Body);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        }).WithName("CreateSupportTicket").WithTags("Support")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.SupportTicket, AppAction.Create))
          .RequireAuthorization();

        group.MapGet("/tickets", async (
            TicketStatus? status,
            TicketCategory? category,
            Guid? cursor,
            int? pageSize,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            bool isAdmin = currentUser.HasPermission("Permission.AdminSupportQueue.Read");
            Guid? userId = isAdmin ? null : currentUser.UserId!.Value;
            var query = new GetSupportTicketsQuery(userId, isAdmin, status, category, cursor, pageSize ?? 20);
            var result = await sender.Send(query, ct);
            return result.ToApiResult();
        }).WithName("GetSupportTickets").WithTags("Support")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.SupportTicket, AppAction.Read))
          .RequireAuthorization();

        group.MapGet("/tickets/{id:guid}", async (
            Guid id,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            bool isAdmin = currentUser.HasPermission("Permission.AdminSupportQueue.Read");
            var result = await sender.Send(new GetSupportTicketByIdQuery(id, currentUser.UserId!.Value, isAdmin), ct);
            return result.ToApiResult();
        }).WithName("GetSupportTicketById").WithTags("Support")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.SupportTicket, AppAction.Read))
          .RequireAuthorization();

        group.MapPost("/tickets/{id:guid}/close", async (
            Guid id,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            bool isAdmin = currentUser.HasPermission("Permission.AdminSupportQueue.Read");
            var result = await sender.Send(new CloseSupportTicketCommand(id, currentUser.UserId!.Value, isAdmin), ct);
            return result.ToApiResult();
        }).WithName("CloseSupportTicket").WithTags("Support")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.SupportTicket, AppAction.Close))
          .RequireAuthorization();

        group.MapPost("/tickets/{id:guid}/messages", async (
            Guid id,
            PostMessageRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            bool isAdmin = currentUser.HasPermission("Permission.AdminSupportQueue.Read");
            var cmd = new PostTicketMessageCommand(id, currentUser.UserId!.Value, request.Body, isAdmin && (request.IsInternal ?? false));
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        }).WithName("PostTicketMessage").WithTags("Support")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.SupportTicket, AppAction.Read))
          .RequireAuthorization();

        group.MapPost("/admin/tickets/{id:guid}/assign", async (
            Guid id,
            AssignTicketRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new AssignSupportTicketCommand(id, request.AdminUserId, currentUser.UserId!.Value);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        }).WithName("AssignSupportTicket").WithTags("Support")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.AdminSupportQueue, AppAction.Assign))
          .RequireAuthorization();

        group.MapPost("/admin/tickets/{id:guid}/resolve", async (
            Guid id,
            ResolveTicketRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new ResolveSupportTicketCommand(id, currentUser.UserId!.Value, request.Notes);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        }).WithName("ResolveSupportTicket").WithTags("Support")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.AdminSupportQueue, AppAction.Resolve))
          .RequireAuthorization();

        return group;
    }
}

internal sealed record CreateTicketRequest(TicketCategory Category, string Subject, string Body);
internal sealed record PostMessageRequest(string Body, bool? IsInternal);
internal sealed record AssignTicketRequest(Guid AdminUserId);
internal sealed record ResolveTicketRequest(string? Notes);
