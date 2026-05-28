using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Commands.CreateBoostPackage;

public sealed record CreateBoostPackageCommand(
    Guid ProviderId,
    EntityType EntityKind,
    Guid EntityId,
    decimal Multiplier,
    DateTime StartsAt,
    DateTime ExpiresAt) : ICommand<CreateBoostPackageResult>;

public sealed record CreateBoostPackageResult(Guid Id);
