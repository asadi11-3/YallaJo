using System.Collections.Concurrent;

namespace Booking.Infrastructure.BackgroundServices;

internal sealed class BookingBackgroundServiceStatusStore : IBookingBackgroundServiceStatusStore
{
    private readonly ConcurrentDictionary<string, BookingBackgroundServiceStatus> _statuses = new(StringComparer.Ordinal);

    public void RecordTickStart(string serviceName, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        _statuses.AddOrUpdate(
            serviceName,
            _ => new BookingBackgroundServiceStatus(
                ServiceName: serviceName,
                LastTickUtc: utcNow,
                LastSuccessUtc: null,
                LastFailureUtc: null,
                LastError: null,
                TotalItemsProcessed: 0),
            (_, prior) => prior with { LastTickUtc = utcNow });
    }

    public void RecordSuccess(string serviceName, DateTimeOffset utcNow, int itemsProcessed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        if (itemsProcessed < 0)
        {
            itemsProcessed = 0;
        }

        _statuses.AddOrUpdate(
            serviceName,
            _ => new BookingBackgroundServiceStatus(
                ServiceName: serviceName,
                LastTickUtc: utcNow,
                LastSuccessUtc: utcNow,
                LastFailureUtc: null,
                LastError: null,
                TotalItemsProcessed: itemsProcessed),
            (_, prior) => prior with
            {
                LastTickUtc = utcNow,
                LastSuccessUtc = utcNow,
                TotalItemsProcessed = prior.TotalItemsProcessed + itemsProcessed,
            });
    }

    public void RecordFailure(string serviceName, DateTimeOffset utcNow, string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        _statuses.AddOrUpdate(
            serviceName,
            _ => new BookingBackgroundServiceStatus(
                ServiceName: serviceName,
                LastTickUtc: utcNow,
                LastSuccessUtc: null,
                LastFailureUtc: utcNow,
                LastError: Truncate(error),
                TotalItemsProcessed: 0),
            (_, prior) => prior with
            {
                LastTickUtc = utcNow,
                LastFailureUtc = utcNow,
                LastError = Truncate(error),
            });
    }

    public BookingBackgroundServiceStatus? GetStatus(string serviceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        return _statuses.TryGetValue(serviceName, out var snapshot) ? snapshot : null;
    }

    public IReadOnlyCollection<BookingBackgroundServiceStatus> GetAllStatuses()
        => _statuses.Values.ToArray();

    private static string Truncate(string error)
        => error.Length <= 500 ? error : error[..500];
}
