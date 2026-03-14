using ContentCore.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Queries.Tag.GetAllTags
{
	internal sealed class GetAllTagsQueryHandler(ITagRepository tagRepository)
		: IQueryHandler<GetAllTagsQuery, List<TagResponse>>
	{
		public async Task<Result<List<TagResponse>>> Handle(GetAllTagsQuery request, CancellationToken cancellationToken)
		{
			// 1. بنجيب كل الداتا (اكتب اسم الميثود اللي عندكم بالـ Repository، غالباً اسمها GetAllAsync أو GetQueryable)
			var allTags = await tagRepository.GetAllAsync();

			// بنحولها لـ IQueryable عشان نقدر نعمل عليها فلترة
			var query = allTags.AsQueryable();

			// 2. الفلترة: إذا اليوزر باعت كلمة بحث، بنفلتر الأسماء
			if (!string.IsNullOrWhiteSpace(request.SearchTerm))
			{
				query = query.Where(t => t.Name.Contains(request.SearchTerm));
			}

			// 3. تقسيم الصفحات (Pagination) والتحويل لـ TagResponse
			var tags = query
				.Skip((request.Page - 1) * request.PageSize)
				.Take(request.PageSize)
				.Select(t => new TagResponse(t.Id, t.Name, t.Slug))
				.ToList();

			// 4. ترجيع النتيجة
			return Result.Success(tags);
		}
	}
	}
