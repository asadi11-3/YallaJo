using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accounts.Application.Queries.Agency.GetAgencies
{
    public sealed record GetAgenciesResult(IReadOnlyList<AgencyListItemDto> Agencies, int TotalCount);
}
