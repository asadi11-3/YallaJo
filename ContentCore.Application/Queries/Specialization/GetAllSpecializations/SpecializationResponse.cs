using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentCore.Application.Queries.Specialization.GetAllSpecializations
{
	public sealed record SpecializationResponse(
	Guid Id,
	string Name,
	string? Description,
	string? Icon,
	bool IsActive);
}
