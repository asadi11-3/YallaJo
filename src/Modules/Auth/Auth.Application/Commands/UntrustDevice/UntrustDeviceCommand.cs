using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.UntrustDevice;

public sealed record UntrustDeviceCommand(Guid DeviceId) : ICommand;
