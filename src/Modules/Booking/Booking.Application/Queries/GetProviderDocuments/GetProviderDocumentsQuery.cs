using Booking.Application.Caching;
using Booking.Application.Commands.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Queries.GetProviderDocuments;

public sealed record GetProviderDocumentsQuery(Guid UserId)
    : IQuery<IReadOnlyList<ProviderDocumentDto>>, ICacheableQuery
{
    public string CacheKey => BookingProviderDocumentCacheKeys.UserListKey(UserId);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags => [BookingProviderDocumentCacheKeys.UserTag(UserId)];
}
