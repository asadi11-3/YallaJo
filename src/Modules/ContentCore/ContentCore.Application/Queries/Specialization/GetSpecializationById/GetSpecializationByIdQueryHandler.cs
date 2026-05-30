using ContentCore.Application.Queries.Specialization.ListSpecializations;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Queries.Specialization.GetSpecializationById;

public sealed class GetSpecializationByIdQueryHandler(
    ISpecializationRepository specializationRepository,
    ILogger<GetSpecializationByIdQueryHandler> logger)
    : IQueryHandler<GetSpecializationByIdQuery, SpecializationDto>
{
    public async Task<Result<SpecializationDto>> Handle(
        GetSpecializationByIdQuery request,
        CancellationToken ct)
    {
        try
        {
            var spec = await specializationRepository.GetByIdAsync(request.Id, ct);

            if (spec is null)
                return Result<SpecializationDto>.Failure(
                    new Error("Specialization.NotFound", $"Specialization '{request.Id}' was not found."),
                    Outcome.NotFound);

            logger.LogDebug("GetSpecializationById: {SpecializationId}", spec.Id);

            return Result<SpecializationDto>.Success(
                new SpecializationDto(spec.Id, spec.Name, spec.Description, spec.Icon, spec.IsActive));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<SpecializationDto>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
