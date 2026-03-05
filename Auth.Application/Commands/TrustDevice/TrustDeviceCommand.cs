using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.TrustDevice;

public sealed record TrustDeviceCommand(Guid DeviceId) : ICommand;
