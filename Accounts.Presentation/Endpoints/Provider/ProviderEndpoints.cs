using Accounts.Application.Commands.Provider.AddDocument;
using Accounts.Application.Commands.Provider.ReapplyProvider;
using Accounts.Application.Commands.Provider.RegisterProvider;
using Accounts.Application.Commands.Provider.ReplaceDocument;
using Accounts.Application.Commands.Provider.SubmitApplication;
using Accounts.Application.Queries.Dashboard;
using Accounts.Application.Queries.GetMyApplicationStatus;
using Accounts.Contracts.Authorization;
using Accounts.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Accounts.Presentation.Endpoints.Provider;

public static class ProviderEndpoints
{
    public static IEndpointRouteBuilder MapProviderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Caller (AccountsEndpoints) already creates the "/api/v1/provider" group with
        // tag "Provider"; do NOT re-prefix here or routes will double-prefix.
        var group = endpoints;

        // GET /api/v1/provider/status — get my application status
        group.MapGet("/status", async (ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        {
            if (currentUser.UserId is null)
                return Results.Unauthorized();

            var result = await sender.Send(new GetMyApplicationStatusQuery(currentUser.UserId.Value), ct);
            return result.ToApiResult();
        })
        .WithName("GetMyProviderStatus")
        .Produces<GetMyApplicationStatusResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get my provider application status")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.ProviderApplication, AppAction.Read))
        .RequireAuthorization();

        // POST /api/v1/provider/register — register a new provider application
        group.MapPost("/register", async (RegisterProviderRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RegisterProviderCommand(
                req.Type,
                req.BusinessName,
                req.ContactEmail,
                req.ContactPhone,
                req.Address,
                req.Description,
                req.TypeSpecificDataJson), ct);
            return result.ToApiResult();
        })
        .WithName("RegisterProvider")
        .Produces<RegisterProviderResult>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithSummary("Register as a provider")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.ProviderApplication, AppAction.Register))
        .RequireAuthorization();

        // POST /api/v1/provider/apply — submit provider application
        group.MapPost("/apply", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SubmitApplicationCommand(), ct);
            return result.ToApiResult();
        })
        .WithName("SubmitProviderApplication")
        .Produces<SubmitApplicationResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithSummary("Submit provider application for review")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.ProviderApplication, AppAction.Submit))
        .RequireAuthorization();

        // POST /api/v1/provider/documents — add a document
        group.MapPost("/documents", async (AddProviderDocumentRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AddProviderDocumentCommand(
                req.DocumentType,
                req.FileUrl,
                req.FileName,
                req.FileSizeBytes,
                req.ExpiresAt), ct);
            return result.ToApiResult();
        })
        .WithName("AddProviderDocument")
        .Produces<AddProviderDocumentResult>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithSummary("Add a document to provider application")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.ProviderApplication, AppAction.Create))
        .RequireAuthorization();

        // PUT /api/v1/provider/documents/{id} — replace a document
        group.MapPut("/documents/{id:guid}", async (Guid id, ReplaceProviderDocumentRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ReplaceProviderDocumentCommand(
                id,
                req.FileUrl,
                req.FileName,
                req.FileSizeBytes,
                req.ExpiresAt), ct);
            return result.ToApiResult();
        })
        .WithName("ReplaceProviderDocument")
        .Produces<ReplaceProviderDocumentResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithSummary("Replace a provider document")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.ProviderApplication, AppAction.Update))
        .RequireAuthorization();

        // GET /api/v1/provider/dashboard/overview — provider dashboard overview
        group.MapGet("/dashboard/overview", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetProviderDashboardOverviewQuery(), ct);
            return result.ToApiResult();
        })
        .WithName("GetProviderDashboardOverview")
        .Produces<ProviderDashboardOverviewResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get provider dashboard overview stats")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.ProviderDashboard, AppAction.Read))
        .RequireAuthorization();

        // GET /api/v1/provider/dashboard/pending-actions — pending actions
        group.MapGet("/dashboard/pending-actions", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetProviderPendingActionsQuery(), ct);
            return result.ToApiResult();
        })
        .WithName("GetProviderPendingActions")
        .Produces<IReadOnlyList<ProviderPendingAction>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get actions requiring provider attention")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.ProviderDashboard, AppAction.Read))
        .RequireAuthorization();

        // POST /api/v1/provider/reapply — reapply after rejection (cooling period must have passed)
        group.MapPost("/reapply", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ReapplyProviderCommand(), ct);
            return result.ToApiResult();
        })
        .WithName("ReapplyProvider")
        .Produces<ReapplyProviderResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithSummary("Reapply after a rejected provider application (cooling period must have passed)")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.ProviderApplication, AppAction.Update))
        .RequireAuthorization();

        // GET /api/v1/provider/dashboard/notifications — recent notifications for the provider
        group.MapGet("/dashboard/notifications", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetProviderNotificationsQuery(), ct);
            return result.ToApiResult();
        })
        .WithName("GetProviderNotifications")
        .Produces<IReadOnlyList<ProviderNotificationDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get recent notifications for the provider (document expiry, status changes)")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.ProviderDashboard, AppAction.Read))
        .RequireAuthorization();

        // GET /api/v1/provider/settings — provider settings
        group.MapGet("/settings", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetProviderSettingsQuery(), ct);
            return result.ToApiResult();
        })
        .WithName("GetProviderSettings")
        .Produces<ProviderSettingsResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get provider settings (business info)")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.ProviderApplication, AppAction.Read))
        .RequireAuthorization();

        return endpoints;
    }
}

// ── Request Models ────────────────────────────────────────────────────────────

public sealed record RegisterProviderRequest(
    ProviderType Type,
    string BusinessName,
    string ContactEmail,
    string ContactPhone,
    string Address,
    string Description,
    string? TypeSpecificDataJson);

public sealed record AddProviderDocumentRequest(
    DocumentType DocumentType,
    string FileUrl,
    string FileName,
    long FileSizeBytes,
    DateTime? ExpiresAt);

public sealed record ReplaceProviderDocumentRequest(
    string FileUrl,
    string FileName,
    long FileSizeBytes,
    DateTime? ExpiresAt);
