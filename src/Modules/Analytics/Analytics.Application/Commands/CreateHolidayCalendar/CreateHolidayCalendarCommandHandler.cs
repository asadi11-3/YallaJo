using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Commands.CreateHolidayCalendar;

public sealed class CreateHolidayCalendarCommandHandler(
    IHolidayCalendarRepository holidayRepository,
    IAnalyticsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<CreateHolidayCalendarCommandHandler> logger) : ICommandHandler<CreateHolidayCalendarCommand, CreateHolidayCalendarResult>
{
    public async Task<Result<CreateHolidayCalendarResult>> Handle(CreateHolidayCalendarCommand request, CancellationToken ct)
    {
        var holiday = HolidayCalendar.Create(request.HolidayName, request.StartDate, request.EndDate, request.Year, request.BoostRulesJson);
        holidayRepository.Add(holiday);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        await cache.RemoveByTagAsync($"analytics:holidays:{request.Year}", ct).ConfigureAwait(false);
        logger.LogInformation("Created analytics holiday calendar {HolidayId} for year {Year}", holiday.Id, request.Year);
        return Result<CreateHolidayCalendarResult>.Success(new CreateHolidayCalendarResult(holiday.Id));
    }
}
