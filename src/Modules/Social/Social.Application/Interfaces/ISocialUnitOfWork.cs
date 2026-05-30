namespace Social.Application.Interfaces;

public interface ISocialUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
