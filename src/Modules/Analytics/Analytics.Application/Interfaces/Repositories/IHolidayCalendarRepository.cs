using Analytics.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Analytics.Application.Interfaces.Repositories;

public interface IHolidayCalendarRepository : IRepository<HolidayCalendar, Guid>
{
    Task<IReadOnlyList<HolidayCalendar>> GetActiveForDateAsync(DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyList<HolidayCalendar>> GetByYearAsync(int year, CancellationToken ct = default);
}
