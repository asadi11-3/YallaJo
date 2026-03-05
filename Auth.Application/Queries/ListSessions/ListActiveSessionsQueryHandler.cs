
using Auth.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Queries.ListSessions;

public sealed class ListActiveSessionsQueryHandler(
    ISessionRepository sessionRepository,
    IDeviceRepository deviceRepository,
    ICurrentUser currentUser)
    : IQueryHandler<ListActiveSessionsQuery, IReadOnlyList<ActiveSessionDto>>
{
    public async Task<Result<IReadOnlyList<ActiveSessionDto>>> Handle(
        ListActiveSessionsQuery request,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<IReadOnlyList<ActiveSessionDto>>.Failure(
                Error.Unauthorized("Authentication is required."),
                Outcome.Unauthorized);

        var userId = currentUser.UserId.Value;

       
        Guid? currentSessionId = null;
        var sidClaim = currentUser.GetClaim("sid");
        if (sidClaim is not null && Guid.TryParse(sidClaim, out var parsedSid))
            currentSessionId = parsedSid;


        var sessions = await sessionRepository.GetActiveSessionsByUserIdAsync(userId, ct);

        if (sessions.Count == 0)
            return Result<IReadOnlyList<ActiveSessionDto>>.Success(
                Array.Empty<ActiveSessionDto>());

      
        var deviceIds = sessions.Select(s => s.DeviceId).Distinct().ToHashSet();
        var devices = await deviceRepository.GetAllAsync(
            filter: d => deviceIds.Contains(d.Id),
            asNoTracking: true,
            ct: ct);

        var deviceMap = devices.ToDictionary(d => d.Id);

       
        IReadOnlyList<ActiveSessionDto> dtos = sessions
            .Select(s =>
            {
                deviceMap.TryGetValue(s.DeviceId, out var device);
                return new ActiveSessionDto(
                    SessionId: s.Id,
                    DeviceId: s.DeviceId,
                    DeviceName: device?.DeviceName,
                    UserAgent: device?.UserAgent,
                    IpAddress: s.IpAddress,
                    CreatedAt: s.CreatedAt,
                    ExpiresAt: s.ExpiresAt,
                    IsCurrent: currentSessionId.HasValue && s.Id == currentSessionId.Value);
            })
            .ToList();

        return Result<IReadOnlyList<ActiveSessionDto>>.Success(dtos);
    }
}
