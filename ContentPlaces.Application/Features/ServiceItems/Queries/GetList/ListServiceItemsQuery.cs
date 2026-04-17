using MediatR;
using System;
using System.Collections.Generic;

namespace ContentPlaces.Application.Features.ServiceItems.Queries.GetList;

public sealed record ListServiceItemsQuery(Guid BusinessId) : IRequest<List<ServiceItemResponse>>;
