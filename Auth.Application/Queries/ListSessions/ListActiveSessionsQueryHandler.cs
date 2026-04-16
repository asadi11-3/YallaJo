using Auth.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Queries.ListSessions;

public sealed class ListActiveSessionsQueryHandler(
    ISessionRepository sessionRepository,
    IDeviceRepository deviceRepository)
    : IQueryHandler<ListActiveSessionsQuery, IReadOnlyList<ActiveSessionListItemDto>>
{
    public async Task<Result<IReadOnlyList<ActiveSessionListItemDto>>> Handle(
        ListActiveSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = request.UserId;

        var now = DateTime.UtcNow;

        var sessions = await sessionRepository.GetAllAsync(
            filter: s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > now,
            orderBy: q => q.OrderByDescending(s => s.CreatedAt),
            asNoTracking: true,
            ct: cancellationToken);

        if (sessions.Count == 0)
            return Result<IReadOnlyList<ActiveSessionListItemDto>>.Success(Array.Empty<ActiveSessionListItemDto>());

        var deviceIds = sessions.Select(s => s.DeviceId).Distinct().ToHashSet();
        var devices = await deviceRepository.GetAllAsync(
            filter: d => deviceIds.Contains(d.Id),
            asNoTracking: true,
            ct: cancellationToken);

        var deviceMap = devices.ToDictionary(d => d.Id);

        var dtos = sessions
            .Select(s =>
            {
                deviceMap.TryGetValue(s.DeviceId, out var device);
                return new ActiveSessionListItemDto(
                    SessionId: s.Id,
                    DeviceId: s.DeviceId,
                    DeviceName: device?.DeviceName,
                    UserAgent: device?.UserAgent,
                    IpAddress: s.IpAddress,
                    CreatedAt: s.CreatedAt,
                    ExpiresAt: s.ExpiresAt);
            })
            .ToList();

        return Result<IReadOnlyList<ActiveSessionListItemDto>>.Success(dtos);
    }
}
