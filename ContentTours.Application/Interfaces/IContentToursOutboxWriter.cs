using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Application.Interfaces;

/// <summary>
/// Stages an integration event on the ContentTours outbox.
/// Non-aggregate command handlers (TourSchedule, TourPricingTier) use this to publish
/// events durably without injecting ContentToursDbContext into the Application layer
/// (which would violate Clean Architecture dependency rules).
///
/// The outbox row is added to the EF change tracker and committed atomically when
/// <see cref="IContentToursUnitOfWork.SaveChangesAsync"/> runs.
/// Do NOT call SaveChangesAsync inside this interface's implementation.
/// </summary>
public interface IContentToursOutboxWriter
{
    void Enqueue(IIntegrationEvent integrationEvent);
}
