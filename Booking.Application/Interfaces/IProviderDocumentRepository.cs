using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Booking.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Booking.Application.Interfaces;

public interface IProviderDocumentRepository
{
    void Add(ProviderDocument document);

    Task<List<ProviderDocument>> GetExpiringWithinAsync(int daysAhead, int batchSize, CancellationToken ct);
    Task<List<ProviderDocument>> GetExpiredAsync(DateTime cutoff, int batchSize, CancellationToken ct);
    Task<ProviderDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ProviderDocument>> GetAllByProviderIdAsync(Guid providerId, CancellationToken ct = default);
}
