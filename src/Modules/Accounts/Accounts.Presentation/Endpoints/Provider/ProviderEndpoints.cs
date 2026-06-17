using Accounts.Application.Commands.Provider.AddDocument;
using Accounts.Application.Commands.Provider.ReapplyProvider;
using Accounts.Application.Commands.Provider.RegisterProvider;
using Accounts.Application.Commands.Provider.ReplaceDocument;
using Accounts.Application.Commands.Provider.SubmitApplication;
using Accounts.Application.Queries.Dashboard;
using Accounts.Application.Queries.DownloadProviderDocument;
using Accounts.Application.Queries.GetMyApplicationStatus;
using Accounts.Contracts.Authorization;
using Accounts.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        // POST /api/v1/provider/documents/upload — upload a document file (multipart)
        group.MapPost("/documents/upload", async (
            [FromForm] UploadProviderDocumentRequest req, ISender sender, CancellationToken ct) =>
        {
            var command = req.TryBuildCommand(out var leasedStream);
            try
            {
                if (command is null)
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["file"] = ["A file is required."],
                    });

                var result = await sender.Send(command, ct);
                return result.ToApiResult();
            }
            finally
            {
                leasedStream?.Dispose();
            }
        })
        .WithName("UploadProviderApplicationDocument")
        .Accepts<UploadProviderDocumentRequest>("multipart/form-data")
        .Produces<AddProviderDocumentResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithSummary("Upload a document file to the provider application")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.ProviderApplication, AppAction.Create))
        .RequireAuthorization()
        .DisableAntiforgery();

        // GET /api/v1/provider/documents/{documentId}/download — authorized, server-mediated download.
        // Streams the file bytes only after verifying the caller owns the parent application
        // (or is admin-tier). The physical path / storage key is never exposed to the client.
        group.MapGet("/documents/{documentId:guid}/download", async (
            Guid documentId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DownloadProviderDocumentQuery(documentId), ct);
            if (result.IsFailure || result.Value is null)
                return result.ToApiResult();

            var download = result.Value;
            // Results.File takes ownership of the stream and disposes it after the response is written.
            return Results.File(download.Content, download.ContentType, download.FileName);
        })
        .WithName("DownloadProviderApplicationDocument")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Download a provider application document (owner or admin only)")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.ProviderApplication, AppAction.Read))
        .RequireAuthorization();

        // POST /api/v1/provider/documents/{id}/replace-upload — replace a document with an uploaded file (multipart).
        group.MapPost("/documents/{id:guid}/replace-upload", async (
            Guid id, [FromForm] ReplaceProviderDocumentUploadRequest req, ISender sender, CancellationToken ct) =>
        {
            var command = req.TryBuildCommand(id, out var leasedStream);
            try
            {
                if (command is null)
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["file"] = ["A file is required."],
                    });

                var result = await sender.Send(command, ct);
                return result.ToApiResult();
            }
            finally
            {
                leasedStream?.Dispose();
            }
        })
        .WithName("ReplaceProviderDocumentUpload")
        .Accepts<ReplaceProviderDocumentUploadRequest>("multipart/form-data")
        .Produces<ReplaceProviderDocumentResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithSummary("Replace a provider document with an uploaded file")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.ProviderApplication, AppAction.Update))
        .RequireAuthorization()
        .DisableAntiforgery();

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
