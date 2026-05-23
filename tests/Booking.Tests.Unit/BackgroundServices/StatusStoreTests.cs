using Booking.Infrastructure.BackgroundServices;
using FluentAssertions;

namespace Booking.Tests.Unit.BackgroundServices;

public sealed class StatusStoreTests
{
    [Fact]
    public void RecordSuccess_accumulates_processed_count_across_calls()
    {
        var store = new BookingBackgroundServiceStatusStore();
        var nowUtc = DateTimeOffset.UtcNow;

        store.RecordTickStart("Svc", nowUtc);
        store.RecordSuccess("Svc", nowUtc, itemsProcessed: 5);
        store.RecordTickStart("Svc", nowUtc.AddMinutes(5));
        store.RecordSuccess("Svc", nowUtc.AddMinutes(5), itemsProcessed: 3);

        var status = store.GetStatus("Svc");
        status.Should().NotBeNull();
        status!.TotalItemsProcessed.Should().Be(8);
        status.LastSuccessUtc.Should().Be(nowUtc.AddMinutes(5));
        status.LastError.Should().BeNull();
    }

    [Fact]
    public void RecordFailure_truncates_long_error_and_stamps_last_failure()
    {
        var store = new BookingBackgroundServiceStatusStore();
        var nowUtc = DateTimeOffset.UtcNow;
        var longError = new string('x', 1_000);

        store.RecordFailure("Svc", nowUtc, longError);

        var status = store.GetStatus("Svc");
        status.Should().NotBeNull();
        status!.LastFailureUtc.Should().Be(nowUtc);
        status.LastError.Should().HaveLength(500);
    }

    [Fact]
    public void GetStatus_returns_null_when_service_never_ticked()
    {
        var store = new BookingBackgroundServiceStatusStore();
        store.GetStatus("Unknown").Should().BeNull();
    }
}
