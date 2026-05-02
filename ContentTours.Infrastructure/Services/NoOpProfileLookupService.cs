using ContentTours.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ContentTours.Infrastructure.Services;

internal sealed class NoOpProfileLookupService(
    ILogger<NoOpProfileLookupService> logger)
    : IProfileLookupService
{
    public Task<PublicProfile?> GetPublicProfileAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "NoOpProfileLookupService.GetPublicProfileAsync called for UserId={UserId}; returning null (stub).",
            userId);
        return Task.FromResult<PublicProfile?>(null);
    }
}
