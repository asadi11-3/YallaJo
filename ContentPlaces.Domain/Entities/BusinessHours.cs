using System;
using YallaJo.SharedKernel.Domain.Entities;
using PlaceDayOfWeek = ContentPlaces.Domain.Enums.DayOfWeek;

namespace ContentPlaces.Domain.Entities;

public sealed class BusinessHours : BaseEntity
{
    private BusinessHours()
    {
    } // EF Core

    public Guid BusinessId { get; private set; }
    public PlaceDayOfWeek DayOfWeek { get; private set; }
    public TimeOnly OpenTime { get; private set; }
    public TimeOnly CloseTime { get; private set; }
    public bool IsClosed { get; private set; }

    public Business Business { get; private set; } = default!;

    public static BusinessHours Create(
        Guid businessId,
        PlaceDayOfWeek dayOfWeek,
        TimeOnly openTime,
        TimeOnly closeTime,
        bool isClosed)
    {
        if (businessId == Guid.Empty)
            throw new ArgumentException("BusinessId cannot be empty.", nameof(businessId));

        if (!isClosed && openTime >= closeTime && !(openTime == TimeOnly.MinValue && closeTime == TimeOnly.MinValue))
        {
            throw new ArgumentException("OpenTime must be before CloseTime unless operating 24 hours.", nameof(openTime));
        }

        return new BusinessHours
        {
            BusinessId = businessId,
            DayOfWeek = dayOfWeek,
            OpenTime = openTime,
            CloseTime = closeTime,
            IsClosed = isClosed
        };
    }

    public void Update(TimeOnly openTime, TimeOnly closeTime, bool isClosed)
    {
        if (!isClosed && openTime >= closeTime && !(openTime == TimeOnly.MinValue && closeTime == TimeOnly.MinValue))
        {
            throw new ArgumentException("OpenTime must be before CloseTime unless operating 24 hours.", nameof(openTime));
        }

        OpenTime = openTime;
        CloseTime = closeTime;
        IsClosed = isClosed;

        UpdatedAt = DateTime.UtcNow;
    }
}
