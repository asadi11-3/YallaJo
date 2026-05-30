using Analytics.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.GetHolidayCalendarByYear;

public sealed class GetHolidayCalendarByYearQueryHandler(
    IHolidayCalendarRepository holidayRepository,
    ILogger<GetHolidayCalendarByYearQueryHandler> logger) : IQueryHandler<GetHolidayCalendarByYearQuery, GetHolidayCalendarByYearResult>
{
    public async Task<Result<GetHolidayCalendarByYearResult>> Handle(GetHolidayCalendarByYearQuery request, CancellationToken ct)
    {
        var holidays = await holidayRepository.GetByYearAsync(request.Year, ct).ConfigureAwait(false);
        var dtos = holidays
            .Select(holiday => new HolidayCalendarDto(holiday.Id, holiday.HolidayName, holiday.StartDate, holiday.EndDate, holiday.Year, holiday.BoostRulesJson, holiday.IsActive))
            .ToList();

        logger.LogDebug("Read {Count} analytics holiday calendar entries for {Year}", dtos.Count, request.Year);
        return Result<GetHolidayCalendarByYearResult>.Success(new GetHolidayCalendarByYearResult(dtos));
    }
}
