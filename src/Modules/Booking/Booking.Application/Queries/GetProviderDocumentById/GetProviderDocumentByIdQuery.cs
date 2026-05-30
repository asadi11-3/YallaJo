using Booking.Application.Caching;
using Booking.Application.Commands.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Queries.GetProviderDocumentById;

public sealed record GetProviderDocumentByIdQuery(Guid Id)
    : IQuery<ProviderDocumentDto>, ICacheableQuery
{
    public string CacheKey => BookingProviderDocumentCacheKeys.ByIdKey(Id);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);

    public IReadOnlyList<string> Tags => [BookingProviderDocumentCacheKeys.DocumentTag(Id)];
}
