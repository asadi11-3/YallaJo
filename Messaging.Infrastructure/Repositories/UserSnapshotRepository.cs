using Messaging.Domain.Entities;
using Messaging.Domain.Repositories;
using Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Messaging.Infrastructure.Repositories;

internal sealed class UserSnapshotRepository(MessagingDbContext context) : IUserSnapshotRepository
{
    public Task<UserSnapshot?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => context.UserSnapshots.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, ct);

    public async Task UpsertAsync(UserSnapshot snapshot, CancellationToken ct = default)
    {
        var existing = await context.UserSnapshots.FirstOrDefaultAsync(u => u.UserId == snapshot.UserId, ct);
        if (existing is null) context.UserSnapshots.Add(snapshot);
        else existing.Update(snapshot.Email, snapshot.FullName, snapshot.LanguageCode);
    }
}
