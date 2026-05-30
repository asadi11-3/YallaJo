using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Application.Interfaces;

/// <summary>
/// Stages an integration event on the ContentPlaces outbox.
/// Non-aggregate command handlers (ServiceItem, BusinessStaff) use this to publish
/// events durably without injecting ContentPlacesDbContext into the Application layer
/// (which would violate gotcha #22 / Clean Architecture dependency rules).
///
/// The outbox row is added to the EF change tracker and committed atomically when
/// <see cref="IContentPlacesUnitOfWork.SaveChangesAsync"/> runs.
/// Do NOT call SaveChangesAsync inside this interface's implementation.
/// </summary>
public interface IContentPlacesOutboxWriter
{
    void Enqueue(IIntegrationEvent integrationEvent);
}
