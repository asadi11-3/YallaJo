using ContentCore.Application.Commands.Specialization.ActivateSpecialization;
using ContentCore.Application.Commands.Specialization.CreateSpecialization;
using ContentCore.Application.Commands.Specialization.DeactivateSpecialization;
using ContentCore.Application.Commands.Specialization.DeleteSpecialization;
using ContentCore.Application.Commands.Specialization.UpdateSpecialization;
using ContentCore.Application.Queries.Specialization.GetSpecializationById;
using ContentCore.Application.Queries.Specialization.ListSpecializations;
using ContentCore.Presentation.Endpoints.Specialization.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ContentCore.Contracts.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.SharedKernel.Application.Authorization;
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

        specializations.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetSpecializationByIdQuery(id), ct);
            return result.ToApiResult();
        })
        .WithName("GetSpecializationById")
        .Produces<SpecializationDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get a specialization by ID")
        .AllowAnonymous();

        specializations.MapPost("/", async (CreateSpecializationRequest request, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new CreateSpecializationCommand(
                request.Name,
                request.Description,
                request.Icon,
                request.SourceLanguageCode), ct);
            return result.ToApiResult();
        })
        .WithName("CreateSpecialization")
        .Produces<CreateSpecializationResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .WithSummary("Create a specialization")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Specialization, AppAction.Create))
        .RequireAuthorization();

        specializations.MapPut("/{id:guid}", async (Guid id, UpdateSpecializationRequest request, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new UpdateSpecializationCommand(
                id,
                request.Name,
                request.Description,
                request.Icon,
                request.IsActive,
                request.SourceLanguageCode), ct);
            return result.ToApiResult();
        })
        .WithName("UpdateSpecialization")
        .Produces<UpdateSpecializationResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Update a specialization")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Specialization, AppAction.Update))
        .RequireAuthorization();

        specializations.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new DeleteSpecializationCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteSpecialization")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Soft-delete a specialization")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Specialization, AppAction.Delete))
        .RequireAuthorization();

        specializations.MapPatch("/{id:guid}/activate", async (Guid id, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new ActivateSpecializationCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("ActivateSpecialization")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Activate a specialization — idempotent, no-op if already active")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Specialization, AppAction.Update))
        .RequireAuthorization();

        specializations.MapPatch("/{id:guid}/deactivate", async (Guid id, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new DeactivateSpecializationCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("DeactivateSpecialization")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Deactivate a specialization — idempotent, no-op if already inactive")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Specialization, AppAction.Update))
        .RequireAuthorization();
    }
}
