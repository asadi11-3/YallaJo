using ContentPlaces.Application.Queries.BusinessAmenity.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.BusinessAmenity.AddBusinessAmenity;

public sealed record AddBusinessAmenityCommand(
    Guid BusinessId,
    string Name,
    string? Icon,
    int SortOrder)
    : ICommand<BusinessAmenityDto>;
