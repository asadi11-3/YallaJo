using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentPlaces.Application.Features.ServiceItems.Commands.Update
{
    public sealed record UpdateServiceItemCommand
    (
        Guid Id,
        Guid BusinessId,
        string Name,
        decimal Price,
        int DurationMinutes,
        int MaxCapacity,
        string Currency,
        int SortOrder
        ) : IRequest<Guid>;
}
