using Finance.Application.Commands.CreateCommissionRule;
using Finance.Application.Commands.DeleteCommissionRule;
using Finance.Application.Commands.UpdateCommissionRule;
using Finance.Application.Queries.Dtos;
using Finance.Application.Queries.GetCommissionRules;
using Finance.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Finance.Presentation.Endpoints.CommissionRule;

internal static class CommissionRuleEndpoints
{
    public static void MapCommissionRuleEndpoints(RouteGroupBuilder group)
    {
        // GET /commissions
        group.MapGet("/", async (
            ISender sender,
            CancellationToken ct,
            [FromQuery] string? tier = null,
            [FromQuery] string? currency = null,
            [FromQuery] bool includeInactive = false) =>
        {
            var result = await sender.Send(new GetCommissionRulesQuery(tier, currency, includeInactive), ct);
            return result.ToApiResult();
        })
        .WithName("GetCommissionRules")
        .WithSummary("Admin: list commission rules.")
        .WithTags("CommissionRules")
        .Produces<IReadOnlyList<CommissionRuleDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.CommissionRule, AppAction.Read))
        .RequireAuthorization();

        // POST /commissions
        group.MapPost("/", async (
            CreateCommissionRuleCommand request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(request, ct);
            return result.ToApiResult();
        })
        .WithName("CreateCommissionRule")
        .WithSummary("Admin: create a commission rule.")
        .WithTags("CommissionRules")
        .Accepts<CreateCommissionRuleCommand>("application/json")
        .Produces<CommissionRuleDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.CommissionRule, AppAction.Create))
        .RequireAuthorization();

        // PUT /commissions/{id}
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateCommissionRuleRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateCommissionRuleCommand(
                id, request.MinMonthlyRevenue, request.MaxMonthlyRevenue, request.Percentage, request.Notes), ct);
            return result.ToApiResult();
        })
        .WithName("UpdateCommissionRule")
        .WithSummary("Admin: update an existing commission rule.")
        .WithTags("CommissionRules")
        .Accepts<UpdateCommissionRuleRequest>("application/json")
        .Produces<CommissionRuleDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.CommissionRule, AppAction.Update))
        .RequireAuthorization();

        // DELETE /commissions/{id}
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct,
            [FromQuery] string? reason = null) =>
        {
            var result = await sender.Send(new DeleteCommissionRuleCommand(id, reason), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteCommissionRule")
        .WithSummary("Admin: soft-delete a commission rule.")
        .WithTags("CommissionRules")
        .Produces<bool>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.CommissionRule, AppAction.Delete))
        .RequireAuthorization();
    }

    public sealed record UpdateCommissionRuleRequest(
        decimal MinMonthlyRevenue,
        decimal? MaxMonthlyRevenue,
        decimal Percentage,
        string? Notes);
}
