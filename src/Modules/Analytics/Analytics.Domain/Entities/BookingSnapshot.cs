using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

public sealed class BookingSnapshot : BaseEntity
{
    private BookingSnapshot() { }
    public Guid BookingId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ProviderId { get; private set; }
    public Guid TourId { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = "JOD";
    public DateTime CreatedAtSnapshot { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    public static BookingSnapshot Create(Guid bookingId, Guid userId, Guid providerId, Guid tourId, string status, decimal totalAmount, string currency, DateTime createdAt)
        => new() { BookingId = bookingId, UserId = userId, ProviderId = providerId, TourId = tourId, Status = status, TotalAmount = totalAmount, Currency = currency, CreatedAtSnapshot = createdAt };

    public void MarkConfirmed() => Status = "Confirmed";

    public void MarkCancelled(DateTime cancelledAt)
    {
        Status = "Cancelled";
        CancelledAt = cancelledAt;
    }

    public void MarkCompleted(DateTime completedAt)
    {
        Status = "Completed";
        CompletedAt = completedAt;
    }
}
