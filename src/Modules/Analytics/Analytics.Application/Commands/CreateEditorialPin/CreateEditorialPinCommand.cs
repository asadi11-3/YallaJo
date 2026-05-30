using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Commands.CreateEditorialPin;

public sealed record CreateEditorialPinCommand(
    EntityType EntityKind,
    Guid EntityId,
    int Position,
    SuggestionContext Context,
    string? BadgeText,
    DateTime? ExpiresAt) : ICommand<CreateEditorialPinResult>;

public sealed record CreateEditorialPinResult(Guid Id);
