using ContentPlaces.Application.Queries.Business.Common;
using ContentPlaces.Domain.Enums;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.Business.GetBusinessHours;

public sealed class GetBusinessHoursQueryHandler(
    IBusinessRepository businessRepository,
    ILogger<GetBusinessHoursQueryHandler> logger)
    : IQueryHandler<GetBusinessHoursQuery, IReadOnlyList<BusinessHoursDto>>
{
    public async Task<Result<IReadOnlyList<BusinessHoursDto>>> Handle(
        GetBusinessHoursQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var business = await businessRepository.FirstOrDefaultAsync(
                filter: b => b.Id == request.BusinessId,
                ct: cancellationToken);

            if (business is null)
            {
                return Result<IReadOnlyList<BusinessHoursDto>>.Failure(
                    new Error("Business.NotFound", $"Business '{request.BusinessId}' was not found."),
                    Outcome.NotFound);
            }

            var canView = business.Status == BusinessStatus.Approved
                || request.IsAdmin
                || (request.UserId.HasValue && business.OwnerId == request.UserId.Value);

            if (!canView)
            {
                return Result<IReadOnlyList<BusinessHoursDto>>.Failure(
                    new Error("Business.NotFound", $"Business '{request.BusinessId}' was not found or you do not have permission to view it."),
                    Outcome.NotFound);
            }

            var hours = await businessRepository.GetBusinessHoursAsync(request.BusinessId, cancellationToken);

            var dtos = hours
                .Select(h => new BusinessHoursDto(
                    Id: h.Id,
                    DayOfWeek: h.DayOfWeek.ToString(),
                    OpenTime: h.IsClosed ? null : h.OpenTime.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture),
                    CloseTime: h.IsClosed ? null : h.CloseTime.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture),
                    IsClosed: h.IsClosed))
                .ToList();

            return Result<IReadOnlyList<BusinessHoursDto>>.Success(dtos);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<IReadOnlyList<BusinessHoursDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
