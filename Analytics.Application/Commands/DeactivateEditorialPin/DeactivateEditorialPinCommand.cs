using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Commands.DeactivateEditorialPin;

public sealed record DeactivateEditorialPinCommand(Guid PinId) : ICommand;
