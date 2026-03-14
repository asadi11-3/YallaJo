using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Specialization.GetAllSpecializations
{
	// بياخذ كلمة البحث، ورقم الصفحة، وعدد العناصر
	public sealed record GetAllSpecializationsQuery(
		string? SearchTerm = null,
		int Page = 1,
		int PageSize = 10) : IQuery<List<SpecializationResponse>>;
}
