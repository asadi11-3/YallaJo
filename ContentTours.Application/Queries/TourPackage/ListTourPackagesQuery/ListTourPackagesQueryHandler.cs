using ContentTours.Application.Queries.TourPackage.Common;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourPackage.ListTourPackages;

public sealed class ListTourPackagesQueryHandler(
    ITourPackageRepository repository,
    ILogger<ListTourPackagesQueryHandler> logger)
    : IQueryHandler<ListTourPackagesQuery, PaginatedResult<TourPackageDto>>
{
    private const int MaxPageSize = 50;

    public async Task<Result<PaginatedResult<TourPackageDto>>> Handle(
        ListTourPackagesQuery request,
        CancellationToken ct)
    {
        try
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

            var allPackages = await repository.GetAllAsync(ct);

            // filtering
            var filtered = allPackages
                .Where(p =>
                    (!request.IsActive.HasValue || p.IsActive == request.IsActive.Value) &&
                    (!request.TourId.HasValue || p.TourId == request.TourId.Value))
                .OrderByDescending(p => p.CreatedAt);

            var total = filtered.Count();

            var items = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new TourPackageDto(
                    p.Id,
                    p.TourId,
                    p.Name,
                    p.Description,
                    p.Price.Amount,
                    p.Currency,
                    p.MaxParticipants,
                    p.ValidFrom,
                    p.ValidTo,
                    p.IsActive,
                    p.CreatedAt
                ))
                .ToList();

            var result = new PaginatedResult<TourPackageDto>(
                items,
                total,
                page,
                pageSize
            );

            return Result.Success(result);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result.Failure<PaginatedResult<TourPackageDto>>(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
