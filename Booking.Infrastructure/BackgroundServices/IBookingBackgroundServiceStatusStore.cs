namespace Booking.Infrastructure.BackgroundServices;

public interface IBookingBackgroundServiceStatusStore
{

    void RecordTickStart(string serviceName, DateTimeOffset utcNow);

    void RecordSuccess(string serviceName, DateTimeOffset utcNow, int itemsProcessed);

    void RecordFailure(string serviceName, DateTimeOffset utcNow, string error);

    BookingBackgroundServiceStatus? GetStatus(string serviceName);

    IReadOnlyCollection<BookingBackgroundServiceStatus> GetAllStatuses();
}

public sealed record BookingBackgroundServiceStatus(
    string ServiceName,
    DateTimeOffset? LastTickUtc,
    DateTimeOffset? LastSuccessUtc,
    DateTimeOffset? LastFailureUtc,
    string? LastError,
    long TotalItemsProcessed);
