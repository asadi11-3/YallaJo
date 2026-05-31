using MediatR;
using Messaging.Application.Commands.AssignSupportTicket;
using Messaging.Application.Commands.CloseSupportTicket;
using Messaging.Application.Commands.CreateSupportTicket;
using Messaging.Application.Commands.PostTicketMessage;
using Messaging.Application.Commands.ResolveSupportTicket;
using Messaging.Application.Queries.Dtos;
using Messaging.Application.Queries.GetSupportTicketById;
using Messaging.Application.Queries.GetSupportTickets;
using Messaging.Contracts.Authorization;
using Messaging.Domain.Enums;
using Messaging.Presentation.Endpoints.SupportTicket.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Messaging.Presentation.Endpoints.SupportTicket;

internal static class SupportTicketEndpoints
{
    internal static void MapSupportTicketEndpoints(RouteGroupBuilder group)
    {
        group.WithTags("Messaging | Support Tickets");

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
        })
        .WithName("CreateSupportTicket")
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .WithSummary("Create a new support ticket.")
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
        })
        .WithName("GetSupportTickets")
        .Produces<SupportTicketPageDto>(StatusCodes.Status200OK)
        .WithSummary("List support tickets (own tickets, or all when admin).")
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
        })
        .WithName("GetSupportTicketById")
        .Produces<SupportTicketDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get a support ticket by id.")
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
        })
        .WithName("CloseSupportTicket")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Close a support ticket.")
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
        })
        .WithName("PostTicketMessage")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Post a message to a support ticket.")
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
        })
        .WithName("AssignSupportTicket")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Assign a support ticket to an admin.")
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
        })
        .WithName("ResolveSupportTicket")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Resolve a support ticket.")
        .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.AdminSupportQueue, AppAction.Resolve))
        .RequireAuthorization();
    }
}
