using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Commands.CreateCpcBoostPackage;

public sealed record CreateCpcBoostPackageCommand(
    Guid ProviderId,
    EntityType EntityKind,
    Guid EntityId,
    decimal BidPerClick,
    decimal DailyBudgetCap,
    DateTime StartsAt,
    DateTime ExpiresAt) : ICommand<CreateCpcBoostPackageResult>;

public sealed record CreateCpcBoostPackageResult(Guid Id);
