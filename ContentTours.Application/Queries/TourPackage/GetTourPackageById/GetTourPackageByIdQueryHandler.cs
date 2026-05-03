using ContentTours.Application.Queries.TourPackage.Common;
using ContentTours.Application.Queries.TourPackage.GetTourPackageById;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

public sealed class GetTourPackageByIdQueryHandler
    : IQueryHandler<GetTourPackageByIdQuery, TourPackageDto>
{
    private readonly ITourPackageRepository _repository;
    private readonly ILogger<GetTourPackageByIdQueryHandler> _logger;

    public GetTourPackageByIdQueryHandler(
        ITourPackageRepository repository,
        ILogger<GetTourPackageByIdQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<TourPackageDto>> Handle(
        GetTourPackageByIdQuery request,
        CancellationToken cancellationToken)
    {
        var package = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (package is null)
        {
            _logger.LogWarning("TourPackage with Id {Id} not found", request.Id);

            return Result.Failure<TourPackageDto>(
                Error.NotFound("TourPackage.NotFound", "Tour package not found"));
        }

        var dto = new TourPackageDto(
            package.Id,
            package.TourId,
            package.Name,
            package.Description,
            package.Price.Amount,
            package.Currency,
            package.MaxParticipants,
            package.ValidFrom,
            package.ValidTo,
            package.IsActive,
            package.CreatedAt
        );

        return Result.Success(dto);
    }
}
