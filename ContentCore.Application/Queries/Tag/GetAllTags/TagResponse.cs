using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentCore.Application.Queries.Tag.GetAllTags
{
	public sealed record TagResponse(Guid Id, string Name, string Slug);
}
