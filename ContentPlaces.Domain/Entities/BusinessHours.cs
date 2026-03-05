using PlaceDayOfWeek = ContentPlaces.Domain.Enums.DayOfWeek;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentPlaces.Domain.Entities;

public sealed class BusinessHours : BaseEntity
{
    private BusinessHours() { } // EF Core

    public Guid BusinessId { get; private set; }
    public PlaceDayOfWeek DayOfWeek { get; private set; }
    public TimeOnly OpenTime { get; private set; }
    public TimeOnly CloseTime { get; private set; }
    public bool IsClosed { get; private set; }

    public Business Business { get; private set; } = default!;
}
