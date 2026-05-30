using MediatR;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Outbox;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace YallaJo.SharedKernel.Infrastructure.Handlers.Outbox;

/// <summary>
/// Replays a dead-lettered outbox message by delegating to the matching module's
/// <see cref="IOutboxCleaner.ReplayDeadLetterAsync"/>.
/// The original dead-lettered row is preserved — only a clone is added.
/// </summary>
internal sealed class ReplayDeadLetterCommandHandler(IServiceScopeFactory scopeFactory)
    : IRequestHandler<ReplayDeadLetterCommand, Result>
{
    public async Task<Result> Handle(ReplayDeadLetterCommand request, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var cleaners = scope.ServiceProvider.GetServices<IOutboxCleaner>();

        var cleaner = cleaners.FirstOrDefault(c =>
            string.Equals(c.ModuleName, request.Module, StringComparison.OrdinalIgnoreCase));

        if (cleaner is null)
            return Result.NotFound($"Module '{request.Module}' not found.");

        var replayed = await cleaner.ReplayDeadLetterAsync(request.MessageId, ct);

        return replayed
            ? Result.Success($"Message {request.MessageId} replayed successfully.")
            : Result.NotFound($"Dead-lettered message {request.MessageId} not found in module '{request.Module}'.");
    }
}
