// <copyright file="RedirectEndpoints.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Presentation.Endpoints.Redirect;

using ContentSeo.Application.Commands.Redirect.CreateRedirect;
using ContentSeo.Application.Commands.Redirect.DeleteRedirect;
using ContentSeo.Application.Queries.Redirect.Common;
using ContentSeo.Application.Queries.Redirect.ListRedirects;
using ContentSeo.Contracts.Authorization;
using ContentSeo.Presentation.Endpoints.Redirect.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.SharedKernel.Presentation;

internal static class RedirectEndpoints
{
    internal static void MapRedirectEndpoints(RouteGroupBuilder group)
    {
        // GET /api/v1/seo/redirects
        group.MapGet("/redirects", async (
            string? oldUrl,
            bool? isActive,
            int? statusCode,
            int page,
            int pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var p = page <= 0 ? 1 : page;
            var ps = pageSize <= 0 ? 50 : pageSize;
            var query = new ListRedirectsQuery(oldUrl, isActive, statusCode, p, ps);
            var result = await sender.Send(query, ct);
            return result.ToApiResult();
        })
        .WithName("ListRedirects")
        .WithSummary("List redirects with optional filters and pagination.")
        .Produces<PaginatedResult<RedirectDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithMetadata(new MustHavePermissionAttribute(ContentSeoFeatures.Redirect, AppAction.Read));

        // POST /api/v1/seo/redirects
        group.MapPost("/redirects", async (
            CreateRedirectRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new CreateRedirectCommand(request.OldUrl, request.NewUrl, request.StatusCode);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult(r => $"/api/v1/seo/redirects/{r.Id}");
        })
        .WithName("CreateRedirect")
        .WithSummary("Create a redirect with automatic chain-flattening.")
        .Produces<CreateRedirectResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentSeoFeatures.Redirect, AppAction.Create));

        // DELETE /api/v1/seo/redirects/{id}
        group.MapDelete("/redirects/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new DeleteRedirectCommand(id);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("DeleteRedirect")
        .WithSummary("Deactivate and soft-delete a redirect.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentSeoFeatures.Redirect, AppAction.Delete));
    }
}
