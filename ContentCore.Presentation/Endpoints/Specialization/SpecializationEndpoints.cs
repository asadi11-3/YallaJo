using ContentCore.Application.Commands.Specialization.CreateSpecialization;
using ContentCore.Application.Commands.Specialization.UpdateSpecialization;
using ContentCore.Application.Queries.Specialization.ListSpecializations;
using ContentCore.Presentation.Endpoints.Specialization.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Presentation;

namespace ContentCore.Presentation.Endpoints.Specialization;

internal static class SpecializationEndpoints
{
    internal static void MapSpecializationEndpoints(RouteGroupBuilder group)
    {
        var specializations = group.MapGroup("/specializations").WithTags("ContentCore | Specializations");

        specializations.MapGet("/", async (ISender sender, CancellationToken ct, bool activeOnly = false) =>
        {
            var result = await sender.Send(new ListSpecializationsQuery(activeOnly), ct);
            return result.ToApiResult();
        })
        .WithName("ListSpecializations")
        .Produces<IReadOnlyList<SpecializationDto>>(StatusCodes.Status200OK)
        .WithSummary("List all specializations")
        .AllowAnonymous();

        specializations.MapPost("/", async (CreateSpecializationRequest request, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new CreateSpecializationCommand(
                request.Name,
                request.Description,
                request.Icon), ct);
            return result.ToApiResult();
        })
        .WithName("CreateSpecialization")
        .Produces<CreateSpecializationResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .WithSummary("Create a specialization")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.Specialization, AppAction.Create))
        .RequireAuthorization();

        specializations.MapPut("/{id:guid}", async (Guid id, UpdateSpecializationRequest request, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new UpdateSpecializationCommand(
                id,
                request.Name,
                request.Description,
                request.Icon,
                request.IsActive), ct);
            return result.ToApiResult();
        })
        .WithName("UpdateSpecialization")
        .Produces<UpdateSpecializationResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Update a specialization")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.Specialization, AppAction.Update))
        .RequireAuthorization();
    }
}
