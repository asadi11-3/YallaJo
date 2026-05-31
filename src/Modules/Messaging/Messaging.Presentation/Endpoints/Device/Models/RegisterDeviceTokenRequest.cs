using Messaging.Domain.Enums;

namespace Messaging.Presentation.Endpoints.Device.Models;

internal sealed record RegisterDeviceTokenRequest(string DeviceId, DevicePlatform Platform, string Token);
