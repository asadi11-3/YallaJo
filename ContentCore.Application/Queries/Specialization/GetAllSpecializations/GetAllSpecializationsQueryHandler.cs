using ContentCore.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Queries.Specialization.GetAllSpecializations
{
	internal sealed class GetAllSpecializationsQueryHandler(ISpecializationRepository specializationRepository)
	: IQueryHandler<GetAllSpecializationsQuery, List<SpecializationResponse>>
	{
		public async Task<Result<List<SpecializationResponse>>> Handle(GetAllSpecializationsQuery request, CancellationToken cancellationToken)
		{
			// 1. جلب الداتا (امسح cancellationToken من الأقواس إذا أعطاك خط أحمر زي المرة الماضية)
			var allSpecializations = await specializationRepository.GetAllAsync();
			var query = allSpecializations.AsQueryable();

			// 2. الفلترة: بنبحث بالاسم
			if (!string.IsNullOrWhiteSpace(request.SearchTerm))
			{
				query = query.Where(s => s.Name.Contains(request.SearchTerm));
			}

			// 3. التقسيم لصفحات والتحويل للنسخة الخفيفة
			var specializations = query
				.Skip((request.Page - 1) * request.PageSize)
				.Take(request.PageSize)
				.Select(s => new SpecializationResponse(s.Id, s.Name, s.Description, s.Icon, s.IsActive))
				.ToList();

			return Result.Success(specializations);
		}
	}
}
