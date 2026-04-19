using ContentPlaces.Application.Commands.BusinessStaff.AddBusinessStaff;
using ContentPlaces.Application.Commands.BusinessStaff.RemoveBusinessStaff;
using ContentPlaces.Application.Queries.BusinessStaff.Common;
using ContentPlaces.Application.Queries.BusinessStaff.ListBusinessStaff;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Presentation;

namespace ContentPlaces.Presentation.Endpoints.BusinessStaff;

internal static class BusinessStaffEndpoints
{
    internal static void MapBusinessStaffEndpoints(RouteGroupBuilder group)
    {
        var staff = group.MapGroup("/places/businesses")
            .WithTags("ContentPlaces | BusinessStaff");

        // GET
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
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("List business staff")
        .WithDescription("Returns all active staff members for a given business")
        .AllowAnonymous();

        // POST
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
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Add staff member")
        .WithDescription("Adds a new staff member to the business if not already added")
        .RequireAuthorization();

        // DELETE (soft delete)
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
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Deactivate staff member")
        .WithDescription("Soft deletes (deactivates) a staff member")
        .RequireAuthorization();
    }
}
