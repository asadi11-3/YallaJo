using ContentPlaces.Application.Commands.BusinessStaff.AddBusinessStaff;
using ContentPlaces.Application.Commands.BusinessStaff.RemoveBusinessStaff;
using ContentPlaces.Application.Queries.BusinessStaff.Common;
using ContentPlaces.Application.Queries.BusinessStaff.ListBusinessStaff;
using ContentPlaces.Presentation.Endpoints.BusinessStaff.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ContentPlaces.Contracts.Authorization;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;

namespace ContentPlaces.Presentation.Endpoints.BusinessStaff;

internal static class BusinessStaffEndpoints
{
    internal static void MapBusinessStaffEndpoints(RouteGroupBuilder group)
    {
        var staff = group.MapGroup("/places/businesses")
            .WithTags("ContentPlaces | BusinessStaff");

        staff.MapGet("/{id:guid}/staff", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ListBusinessStaffQuery(id), ct);
            return result.ToApiResult();
        })
        .WithName("ListBusinessStaff")
        .Produces<IReadOnlyList<BusinessStaffDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("List business staff members")
        .AllowAnonymous();

        staff.MapPost("/{id:guid}/staff", async (
            Guid id,
            AddBusinessStaffRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new AddBusinessStaffCommand(id, request.UserId, request.Role), ct);
            return result.ToApiResult();
        })
        .WithName("AddBusinessStaff")
        .Produces<BusinessStaffDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Add a staff member to a business")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.BusinessStaff, AppAction.Create))
        .RequireAuthorization();

        staff.MapDelete("/staff/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new RemoveBusinessStaffCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("RemoveBusinessStaff")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Deactivate a staff member (soft delete)")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.BusinessStaff, AppAction.Delete))
        .RequireAuthorization();
    }
}
