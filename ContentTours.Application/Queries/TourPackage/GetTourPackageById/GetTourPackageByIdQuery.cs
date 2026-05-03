using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using ContentTours.Application.Queries.TourPackage.Common;

namespace ContentTours.Application.Queries.TourPackage.GetTourPackageById;

public sealed record GetTourPackageByIdQuery(Guid Id)
    : IQuery<TourPackageDto>;
