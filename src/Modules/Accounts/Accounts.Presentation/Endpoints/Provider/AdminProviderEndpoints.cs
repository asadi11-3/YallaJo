using Accounts.Application.Commands.Admin.ApproveProvider;
using Accounts.Application.Commands.Admin.RejectProvider;
using Accounts.Application.Commands.Admin.ReinstateProvider;
using Accounts.Application.Commands.Admin.RequestMoreDocs;
using Accounts.Application.Commands.Admin.SuspendProvider;
using Accounts.Application.Queries.GetAdminProviderApplicationById;
using Accounts.Application.Queries.GetAdminProviderQueue;
using Accounts.Contracts.Authorization;
using Accounts.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Accounts.Presentation.Endpoints.Provider;

public static class AdminProviderEndpoints
{
    public static IEndpointRouteBuilder MapAdminProviderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Caller (AccountsEndpoints) already creates the "/api/v1/admin/providers" group
        // with tag "Admin - Providers"; do NOT re-prefix here or routes will double-prefix.
        var group = endpoints;

        // GET /api/v1/admin/providers — get provider queue
        group.MapGet("/", async (
            ProviderApplicationStatus? status,
            ProviderType? type,
            int page,
            int pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize is <= 0 or > 100 ? 20 : pageSize;
            var result = await sender.Send(new GetAdminProviderQueueQuery(status, type, page, pageSize), ct);
            return result.ToApiResult();
        })
        .WithName("GetAdminProviderQueue")
        .Produces<GetAdminProviderQueueResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithSummary("Get the provider application queue (admin)")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.AdminProviderQueue, AppAction.Read))
        .RequireAuthorization();

        // GET /api/v1/admin/providers/{id} — get a single provider application (admin detail)
        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAdminProviderApplicationByIdQuery(id), ct);
            return result.ToApiResult();
        })
        .WithName("GetAdminProviderApplicationById")
        .Produces<AdminProviderApplicationDetailsResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get a provider application with documents and review details (admin)")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.AdminProviderQueue, AppAction.Read))
        .RequireAuthorization();

        // POST /api/v1/admin/providers/{id}/approve
        group.MapPost("/{id:guid}/approve", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ApproveProviderCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("ApproveProvider")
        .Produces<ApproveProviderResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithSummary("Approve a provider application")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.AdminProviderQueue, AppAction.Approve))
        .RequireAuthorization();

        // POST /api/v1/admin/providers/{id}/reject
        group.MapPost("/{id:guid}/reject", async (Guid id, RejectProviderRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RejectProviderCommand(id, req.Reason), ct);
            return result.ToApiResult();
        })
        .WithName("RejectProvider")
        .Produces<RejectProviderResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithSummary("Reject a provider application")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.AdminProviderQueue, AppAction.Reject))
        .RequireAuthorization();

        // POST /api/v1/admin/providers/{id}/request-docs
        group.MapPost("/{id:guid}/request-docs", async (Guid id, RequestMoreDocsRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RequestMoreDocsCommand(id, req.MissingDocumentTypes, req.Notes), ct);
            return result.ToApiResult();
        })
        .WithName("RequestMoreProviderDocs")
        .Produces<RequestMoreDocsResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithSummary("Request additional documents from provider")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.AdminProviderQueue, AppAction.RequestDocs))
        .RequireAuthorization();

        // POST /api/v1/admin/providers/{id}/suspend
        group.MapPost("/{id:guid}/suspend", async (Guid id, SuspendProviderRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SuspendProviderCommand(id, req.Reason), ct);
            return result.ToApiResult();
        })
        .WithName("SuspendProvider")
        .Produces<SuspendProviderResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithSummary("Suspend an approved provider")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.AdminProviderQueue, AppAction.Suspend))
        .RequireAuthorization();

        // POST /api/v1/admin/providers/{id}/reinstate
        group.MapPost("/{id:guid}/reinstate", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ReinstateProviderCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("ReinstateProvider")
        .Produces<ReinstateProviderResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithSummary("Reinstate a suspended provider")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.AdminProviderQueue, AppAction.Reinstate))
        .RequireAuthorization();

        return endpoints;
    }
}


public sealed record RejectProviderRequest(string Reason);

public sealed record RequestMoreDocsRequest(
    IReadOnlyList<DocumentType> MissingDocumentTypes,
    string Notes);

public sealed record SuspendProviderRequest(string Reason);
