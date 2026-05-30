using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Analytics.Infrastructure.Repositories;

internal sealed class HolidayCalendarRepository(AnalyticsDbContext context) : EfRepository<HolidayCalendar, Guid>(context), IHolidayCalendarRepository
{
    public async Task<IReadOnlyList<HolidayCalendar>> GetActiveForDateAsync(DateOnly date, CancellationToken ct = default)
        => await context.Set<HolidayCalendar>()
            .Where(x => x.IsActive && x.StartDate <= date && x.EndDate >= date)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<HolidayCalendar>> GetByYearAsync(int year, CancellationToken ct = default)
        => await context.Set<HolidayCalendar>()
            .Where(x => x.Year == year)
            .OrderBy(x => x.StartDate)
            .ToListAsync(ct);
}
