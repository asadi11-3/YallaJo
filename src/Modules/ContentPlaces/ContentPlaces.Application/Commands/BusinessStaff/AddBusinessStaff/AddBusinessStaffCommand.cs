using ContentPlaces.Application.Queries.BusinessStaff.Common;
using ContentPlaces.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.BusinessStaff.AddBusinessStaff;

public sealed record AddBusinessStaffCommand(
    Guid BusinessId,
    Guid ActingUserId,
    Guid UserId,
    BusinessStaffRole Role)
    : ICommand<BusinessStaffDto>;
