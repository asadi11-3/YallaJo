using Finance.Application.Commands.InitiatePayment;
using Finance.Application.Commands.ProcessWebhook;
using Finance.Application.Commands.RefundPayment;
using Finance.Application.Queries.Dtos;
using Finance.Application.Queries.GetAdminPayments;
using Finance.Application.Queries.GetMyPayments;
using Finance.Application.Queries.GetPaymentById;
using Finance.Contracts.Authorization;
using Finance.Contracts.Services;
using Finance.Domain.Enums;
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

namespace Finance.Presentation.Endpoints.Payment;

/// <summary>
/// Payment endpoints (T1 + T2). Mounted under <c>/api/v1/payments</c>.
/// </summary>
internal static class PaymentEndpoints
{
    private const string AdminPermissionName = "Permission.AdminFinanceDashboard.Read";

    internal static void MapPaymentEndpoints(RouteGroupBuilder group)
    {
        MapInitiatePaymentEndpoint(group);
        MapWebhookEndpoint(group);
        MapRefundEndpoint(group);
        MapGetByIdEndpoint(group);
        MapMyPaymentsEndpoint(group);
        MapAdminAllEndpoint(group);
    }

    private static void MapInitiatePaymentEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/initiate", async (
                InitiatePaymentRequest request,
                ICurrentUser currentUser,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request.ToCommand(currentUser.UserId!.Value), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("InitiatePayment")
            .WithSummary("Initiate a payment for an existing booking.")
            .WithTags("Payments")
            .Accepts<InitiatePaymentRequest>("application/json")
            .Produces<InitiatePaymentResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.Payment, AppAction.Create))
            .RequireAuthorization();
    }

    /// <summary>
    /// POST /webhook — F-R2 HMAC verified BEFORE body parse. F-R3 idempotent via inbox.
    /// </summary>
    private static void MapWebhookEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/webhook", async (
                HttpRequest httpRequest,
                IPaymentGateway gateway,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                // 1) Read raw body for HMAC verification
                httpRequest.EnableBuffering();
                using var reader = new StreamReader(httpRequest.Body, leaveOpen: true);
                var rawBody = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                httpRequest.Body.Position = 0;

                var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var h in httpRequest.Headers)
                {
                    headers[h.Key] = h.Value.ToString();
                }

                // 2) Verify HMAC FIRST
                var sigValid = await gateway
                    .VerifyWebhookSignatureAsync(rawBody, headers, cancellationToken)
                    .ConfigureAwait(false);

                if (!sigValid)
                {
                    return Result.Failure<ProcessWebhookResult>(
                            new Error("Payment.WebhookSignatureMismatch", "Invalid webhook signature."),
                            Outcome.Invalid)
                        .ToApiResult();
                }

                // 3) Parse envelope
                WebhookEnvelope? envelope;
                try
                {
                    envelope = System.Text.Json.JsonSerializer.Deserialize<WebhookEnvelope>(rawBody,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (Exception)
                {
                    return Result.Failure<ProcessWebhookResult>(
                            new Error("Payment.WebhookEventUnknown", "Cannot parse webhook envelope."),
                            Outcome.Invalid)
                        .ToApiResult();
                }

                if (envelope is null || envelope.Data is null)
                {
                    return Result.Failure<ProcessWebhookResult>(
                            new Error("Payment.WebhookEventUnknown", "Webhook envelope or data missing."),
                            Outcome.Invalid)
                        .ToApiResult();
                }

                var command = new ProcessWebhookCommand(
                    EventId: envelope.EventId ?? Guid.NewGuid().ToString(),
                    EventType: envelope.EventType ?? string.Empty,
                    OccurredAt: envelope.OccurredAt ?? DateTime.UtcNow,
                    GatewayPaymentId: envelope.Data.GatewayPaymentId ?? string.Empty,
                    Amount: envelope.Data.Amount ?? 0m,
                    Currency: envelope.Data.Currency ?? string.Empty,
                    FailureCode: envelope.Data.FailureCode,
                    FailureMessage: envelope.Data.FailureMessage,
                    Source: headers.TryGetValue("X-Gateway", out var gw) ? gw : "Unknown");

                var result = await sender.Send(command, cancellationToken);
                return result.ToApiResult();
            })
            .WithName("PaymentWebhook")
            .WithSummary("Receives signed webhook events from the payment gateway (HMAC verified before parse).")
            .WithTags("Payments")
            .Produces<ProcessWebhookResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .AllowAnonymous();
    }

    /// <summary>
    /// POST /{id}/refund — F-R5 self/provider/admin guarded.
    /// </summary>
    private static void MapRefundEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/refund", async (
                Guid id,
                RefundPaymentRequest request,
                ICurrentUser currentUser,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var isAdmin = currentUser.HasPermission(AdminPermissionName);
                var providerId = TryParseProviderClaim(currentUser);

                var command = new RefundPaymentCommand(
                    PaymentId: id,
                    Amount: request.Amount,
                    Currency: request.Currency,
                    Reason: request.Reason,
                    CallerUserId: currentUser.UserId!.Value,
                    CallerIsAdmin: isAdmin,
                    CallerProviderId: providerId);

                var result = await sender.Send(command, cancellationToken);
                return result.ToApiResult();
            })
            .WithName("RefundPayment")
            .WithSummary("Refund a completed Booking payment.")
            .WithTags("Payments")
            .Accepts<RefundPaymentRequest>("application/json")
            .Produces<RefundPaymentResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.Refund, AppAction.Create))
            .RequireAuthorization();
    }

    /// <summary>
    /// GET /{id} — read a single payment. Owner self / provider self / admin.
    /// </summary>
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
                    return Result.Failure<PaymentDto>(
                            new Error("Payment.Unauthorized", "Authentication is required."),
                            Outcome.Unauthorized)
                        .ToApiResult();
                }

                var query = new GetPaymentByIdQuery(
                    PaymentId: id,
                    CallerUserId: currentUser.UserId.Value,
                    CallerIsAdmin: currentUser.HasPermission(AdminPermissionName),
                    CallerProviderId: TryParseProviderClaim(currentUser));

                var result = await sender.Send(query, cancellationToken);
                return result.ToApiResult();
            })
            .WithName("GetPaymentById")
            .WithSummary("Read a single payment row.")
            .WithTags("Payments")
            .Produces<PaymentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.Payment, AppAction.Read))
            .RequireAuthorization();
    }

    /// <summary>
    /// GET /my-payments — caller-scoped cursor pagination.
    /// </summary>
    private static void MapMyPaymentsEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/my-payments", async (
                [FromQuery] Guid? cursor,
                [FromQuery] int? pageSize,
                ICurrentUser currentUser,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                {
                    return Result.Failure<PaymentPageDto>(
                            new Error("Payment.Unauthorized", "Authentication is required."),
                            Outcome.Unauthorized)
                        .ToApiResult();
                }

                var query = new GetMyPaymentsQuery(
                    CallerUserId: currentUser.UserId.Value,
                    Cursor: cursor,
                    PageSize: pageSize ?? 20);

                var result = await sender.Send(query, cancellationToken);
                return result.ToApiResult();
            })
            .WithName("GetMyPayments")
            .WithSummary("Paginated list of the caller's own payments.")
            .WithTags("Payments")
            .Produces<PaymentPageDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.Payment, AppAction.Read))
            .RequireAuthorization();
    }

    /// <summary>
    /// GET /admin/all — admin-only paginated list with filters.
    /// </summary>
    private static void MapAdminAllEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/admin/all", async (
                [FromQuery] Guid? userId,
                [FromQuery] Guid? providerId,
                [FromQuery] PaymentStatus? status,
                [FromQuery] PaymentType? type,
                [FromQuery] Guid? cursor,
                [FromQuery] int? pageSize,
                [FromQuery] bool? countTotal,
                ICurrentUser currentUser,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                {
                    return Result.Failure<PaymentPageDto>(
                            new Error("Payment.Unauthorized", "Authentication is required."),
                            Outcome.Unauthorized)
                        .ToApiResult();
                }

                var query = new GetAdminPaymentsQuery(
                    UserId: userId,
                    ProviderId: providerId,
                    Status: status,
                    Type: type,
                    Cursor: cursor,
                    PageSize: pageSize ?? 20,
                    CountTotal: countTotal ?? false);

                var result = await sender.Send(query, cancellationToken);
                return result.ToApiResult();
            })
            .WithName("GetAdminPayments")
            .WithSummary("Admin-only paginated list of payments with filters.")
            .WithTags("Payments")
            .Produces<PaymentPageDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.AdminFinanceDashboard, AppAction.Read))
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

internal sealed record WebhookEnvelope(
    string? EventId,
    string? EventType,
    DateTime? OccurredAt,
    WebhookEnvelopeData? Data);

internal sealed record WebhookEnvelopeData(
    string? GatewayPaymentId,
    decimal? Amount,
    string? Currency,
    string? FailureCode,
    string? FailureMessage);
