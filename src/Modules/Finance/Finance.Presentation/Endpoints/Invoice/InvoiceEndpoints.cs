using Finance.Application.Queries.DownloadInvoice;
using Finance.Application.Queries.Dtos;
using Finance.Application.Queries.GetInvoiceById;
using Finance.Application.Queries.GetMyInvoices;
using Finance.Application.Queries.GetProviderInvoices;
using Finance.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Finance.Presentation.Endpoints.Invoice;

/// <summary>
/// Invoice endpoints (T3, F-R8). Mounted under <c>/api/v1/invoices</c>.
/// </summary>
internal static class InvoiceEndpoints
{
    private const string AdminPermissionName = "Permission.AdminFinanceDashboard.Read";

    internal static void MapInvoiceEndpoints(RouteGroupBuilder group)
    {
        MapMyInvoicesEndpoint(group);
        MapProviderInvoicesEndpoint(group);
        MapGetByIdEndpoint(group);
        MapDownloadEndpoint(group);
    }

    private static void MapMyInvoicesEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/my-invoices", async (
                [FromQuery] Guid? cursor,
                [FromQuery] int? pageSize,
                ICurrentUser currentUser,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                {
                    return Result.Failure<InvoicePageDto>(
                            new Error("Invoice.Unauthorized", "Authentication is required."),
                            Outcome.Unauthorized)
                        .ToApiResult();
                }

                var query = new GetMyInvoicesQuery(currentUser.UserId.Value, cursor, pageSize ?? 20);
                var result = await sender.Send(query, cancellationToken);
                return result.ToApiResult();
            })
            .WithName("GetMyInvoices")
            .WithSummary("Paginated list of invoices owned by the caller (buyer).")
            .WithTags("Invoices")
            .Produces<InvoicePageDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.Invoice, AppAction.Read))
            .RequireAuthorization();
    }

    private static void MapProviderInvoicesEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/provider/my-invoices", async (
                [FromQuery] Guid? cursor,
                [FromQuery] int? pageSize,
                ICurrentUser currentUser,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                {
                    return Result.Failure<InvoicePageDto>(
                            new Error("Invoice.Unauthorized", "Authentication is required."),
                            Outcome.Unauthorized)
                        .ToApiResult();
                }

                var providerId = TryParseProviderClaim(currentUser);
                if (providerId is null)
                {
                    return Result.Failure<InvoicePageDto>(
                            new Error("Invoice.OwnerMismatch", "Caller is not a provider."),
                            Outcome.Forbidden)
                        .ToApiResult();
                }

                var query = new GetProviderInvoicesQuery(providerId.Value, cursor, pageSize ?? 20);
                var result = await sender.Send(query, cancellationToken);
                return result.ToApiResult();
            })
            .WithName("GetProviderInvoices")
            .WithSummary("Paginated list of invoices issued to the caller's provider account.")
            .WithTags("Invoices")
            .Produces<InvoicePageDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.Invoice, AppAction.Read))
            .RequireAuthorization();
    }

    private static void MapGetByIdEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (
                Guid id,
                ICurrentUser currentUser,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                {
                    return Result.Failure<InvoiceDto>(
                            new Error("Invoice.Unauthorized", "Authentication is required."),
                            Outcome.Unauthorized)
                        .ToApiResult();
                }

                var query = new GetInvoiceByIdQuery(
                    id,
                    currentUser.UserId.Value,
                    currentUser.HasPermission(AdminPermissionName),
                    TryParseProviderClaim(currentUser));

                var result = await sender.Send(query, cancellationToken);
                return result.ToApiResult();
            })
            .WithName("GetInvoiceById")
            .WithSummary("Read a single invoice.")
            .WithTags("Invoices")
            .Produces<InvoiceDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.Invoice, AppAction.Read))
            .RequireAuthorization();
    }

    private static void MapDownloadEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}/download", async (
                Guid id,
                ICurrentUser currentUser,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                {
                    return Result.Failure<DownloadInvoiceResult>(
                            new Error("Invoice.Unauthorized", "Authentication is required."),
                            Outcome.Unauthorized)
                        .ToApiResult();
                }

                var query = new DownloadInvoiceQuery(
                    id,
                    currentUser.UserId.Value,
                    currentUser.HasPermission(AdminPermissionName),
                    TryParseProviderClaim(currentUser));

                var result = await sender.Send(query, cancellationToken);
                if (!result.IsSuccess)
                {
                    return result.ToApiResult();
                }

                var v = result.Value;
                return Results.File(v.PdfBytes, v.ContentType, v.FileName);
            })
            .WithName("DownloadInvoice")
            .WithSummary("Download the rendered invoice PDF (lazily rendered on first call).")
            .WithTags("Invoices")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.Invoice, AppAction.Download))
            .RequireAuthorization();
    }

    private static Guid? TryParseProviderClaim(ICurrentUser user)
    {
        var raw = user.GetClaim("provider_id");
        if (string.IsNullOrEmpty(raw))
        {
            return null;
        }

        return Guid.TryParse(raw, out var pid) ? pid : null;
    }
}
