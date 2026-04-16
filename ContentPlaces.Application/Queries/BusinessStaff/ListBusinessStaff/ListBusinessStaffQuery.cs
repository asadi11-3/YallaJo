using ContentPlaces.Application.Queries.BusinessStaff.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.BusinessStaff.ListBusinessStaff;

public sealed record ListBusinessStaffQuery(Guid BusinessId)
    : IQuery<IReadOnlyList<BusinessStaffDto>>;
