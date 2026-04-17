using ContentPlaces.Domain.Repositories;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Abstractions.Context; // عشان الـ ICurrentUser

namespace ContentPlaces.Application.Features.ServiceItems.Queries.GetList;

public sealed class ListServiceItemsQueryHandler : IRequestHandler<ListServiceItemsQuery, List<ServiceItemResponse>>
{
    private readonly IServiceItemRepository _repository;
    public ListServiceItemsQueryHandler(
        IServiceItemRepository repository)
    {
        _repository = repository;
    }

    public Task<List<ServiceItemResponse>> Handle(ListServiceItemsQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
