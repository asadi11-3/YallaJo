using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Queries.Dashboard;

/// <summary>Returns a list of actionable items the provider must address.</summary>
public sealed record GetProviderPendingActionsQuery : IQuery<IReadOnlyList<ProviderPendingAction>>;

public sealed record ProviderPendingAction(
    string ActionType,
    string Description,
    string? EntityId,
    DateTime? Deadline);
