using Finance.Application.ProviderPaymentMethods;
using Finance.Contracts.Authorization;
using Finance.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Finance.Presentation.Endpoints.ProviderPaymentMethod;

internal static class ProviderPaymentMethodEndpoints
{
    internal static void MapProviderPaymentMethodEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/", async (ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetProviderPaymentMethodsQuery(currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        })
        .WithName("GetProviderPaymentMethods")
        .WithTags("Provider Payment Methods")
        .Produces<IReadOnlyList<ProviderPaymentMethodDto>>(StatusCodes.Status200OK)
        .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.ProviderPaymentMethod, AppAction.Read))
        .RequireAuthorization();

        group.MapPost("/", async (ProviderPaymentMethodRequest request, ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        {
            var command = new CreateProviderPaymentMethodCommand(
                currentUser.UserId!.Value,
                request.PaymentMethodType,
                request.DisplayName,
                request.AccountIdentifier,
                request.BankName,
                request.IsDefault);

            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        })
        .WithName("CreateProviderPaymentMethod")
        .WithTags("Provider Payment Methods")
        .Produces<ProviderPaymentMethodDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.ProviderPaymentMethod, AppAction.Create))
        .RequireAuthorization();

        group.MapPut("/{id:guid}", async (Guid id, ProviderPaymentMethodRequest request, ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        {
            var command = new UpdateProviderPaymentMethodCommand(
                id,
                currentUser.UserId!.Value,
                request.PaymentMethodType,
                request.DisplayName,
                request.AccountIdentifier,
                request.BankName,
                request.IsDefault);

            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        })
        .WithName("UpdateProviderPaymentMethod")
        .WithTags("Provider Payment Methods")
        .Produces<ProviderPaymentMethodDto>(StatusCodes.Status200OK)
        .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.ProviderPaymentMethod, AppAction.Update))
        .RequireAuthorization();

        group.MapDelete("/{id:guid}", async (Guid id, ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteProviderPaymentMethodCommand(id, currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteProviderPaymentMethod")
        .WithTags("Provider Payment Methods")
        .Produces<bool>(StatusCodes.Status200OK)
        .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.ProviderPaymentMethod, AppAction.Delete))
        .RequireAuthorization();

        group.MapPost("/{id:guid}/verify", async (Guid id, VerifyProviderPaymentMethodRequest request, ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new VerifyProviderPaymentMethodCommand(id, currentUser.UserId!.Value, request.IsVerified), ct);
            return result.ToApiResult();
        })
        .WithName("VerifyProviderPaymentMethod")
        .WithTags("Provider Payment Methods")
        .Produces<ProviderPaymentMethodDto>(StatusCodes.Status200OK)
        .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.ProviderPaymentMethod, AppAction.Verify))
        .RequireAuthorization();
    }
}

public sealed record ProviderPaymentMethodRequest(
    ProviderPaymentMethodType PaymentMethodType,
    string DisplayName,
    string AccountIdentifier,
    string? BankName,
    bool IsDefault);

public sealed record VerifyProviderPaymentMethodRequest(bool IsVerified);
