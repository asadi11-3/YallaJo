using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Tag.GetAllTags
{
	// هاد الطلب بياخذ كلمة البحث، ورقم الصفحة، وعدد العناصر بالصفحة
	public sealed record GetAllTagsQuery(
		string? SearchTerm = null,
		int Page = 1,
		int PageSize = 10) : IQuery<List<TagResponse>>;
}
