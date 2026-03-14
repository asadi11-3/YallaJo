using ContentCore.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Data;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Specialization.UpdateSpecialization
{
	internal sealed class UpdateSpecializationCommandHandler(
	ISpecializationRepository specializationRepository,
	YallaJo.SharedKernel.Domain.Abstractions.Data.IUnitOfWork unitOfWork) : ICommandHandler<UpdateSpecializationCommand, Guid>
	{
		public async Task<Result<Guid>> Handle(UpdateSpecializationCommand request, CancellationToken cancellationToken)
		{
			// 1. ندور على التخصص (شيل الـ cancellationToken إذا أعطاك خط أحمر زي العادة)
			var specialization = await specializationRepository.GetByIdAsync(request.Id);

			if (specialization is null)
			{
				return Result.Failure<Guid>(new Error("Specialization.NotFound", "التخصص غير موجود."));
			}

			// 2. نتأكد إنه الاسم الجديد مش مستخدم لتخصص "ثاني"
			bool isDuplicate = await specializationRepository.AnyAsync(
				s => s.Name == request.Name && s.Id != request.Id,
				cancellationToken);

			if (isDuplicate)
			{
				return Result.Failure<Guid>(new Error("Specialization.Duplicate", "اسم التخصص مستخدم مسبقاً."));
			}

			// 3. التعديل الفعلي (لازم تحط المسار الكامل إذا عمللك مشكلة تضارب الأسماء)
			// ContentCore.Domain.Entities.Specialization
			specialization.Update(request.Name, request.Description, request.Icon, request.IsActive);

			// 4. الحفظ
			await unitOfWork.SaveChangesAsync(cancellationToken);

			return Result.Success(specialization.Id);
		}
	}
}
