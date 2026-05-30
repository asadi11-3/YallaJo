using MediatR;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Application.Abstractions.Outbox;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace YallaJo.SharedKernel.Infrastructure.Handlers.Outbox;

/// <summary>
/// Returns dead-lettered outbox messages across all (or a specific) module.
/// </summary>
internal sealed class ListDeadLettersQueryHandler(IServiceScopeFactory scopeFactory)
    : IRequestHandler<ListDeadLettersQuery, Result<ListDeadLettersResult>>
{
    public async Task<Result<ListDeadLettersResult>> Handle(
        ListDeadLettersQuery request,
        CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var cleaners = scope.ServiceProvider.GetServices<IOutboxCleaner>();

        if (!string.IsNullOrEmpty(request.Module))
        {
            cleaners = cleaners.Where(c =>
                string.Equals(c.ModuleName, request.Module, StringComparison.OrdinalIgnoreCase));
        }

        var allItems = new List<DeadLetterEntry>();

        foreach (var cleaner in cleaners)
        {
            var dtos = await cleaner.ListDeadLetteredAsync(request.Limit, ct);
            allItems.AddRange(dtos.Select(d => new DeadLetterEntry(
                d.Id, d.Module, d.Type, d.OccurredOnUtc, d.RetryCount, d.LastError)));
        }

        var sorted = allItems
            .OrderByDescending(x => x.OccurredOnUtc)
            .ToList();

        return Result.Success(new ListDeadLettersResult(sorted, sorted.Count));
    }
}
