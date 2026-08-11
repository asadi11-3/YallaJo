namespace Tracking.Application.Interfaces;

/// <summary>
/// Unit-of-work facade for the Tracking bounded context.
/// Delegates to <see cref="YallaJo.SharedKernel.Infrastructure.Data.IUnitOfWork{TrackingDbContext}"/>
/// so that domain-event dispatch (and Outbox writes) are guaranteed before every commit.
/// </summary>
public interface ITrackingUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
