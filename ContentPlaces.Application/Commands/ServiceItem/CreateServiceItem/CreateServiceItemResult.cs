using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentPlaces.Application.Commands.ServiceItem.CreateServiceItem
{
    public sealed record CreateServiceItemResult(Guid ServiceItemId, string Name);
}
