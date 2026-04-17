using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentPlaces.Application.Features.ServiceItems.Commands.Delete
{
    public sealed record DeleteServiceItemCommand(Guid Id, Guid BusinessId) : IRequest;
}
