using ContentCore.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Data;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Specialization.CreateTag
{
	internal sealed class CreateSpecializationCommandHandler(
	ISpecializationRepository specializationRepository,
	YallaJo.SharedKernel.Domain.Abstractions.Data.IUnitOfWork unitOfWork) : ICommandHandler<CreateSpecializationCommand, Guid>
	{
		public async Task<Result<Guid>> Handle(CreateSpecializationCommand request, CancellationToken cancellationToken)
		{
			// 1. فحص التكرار (على الاسم فقط)
			bool isDuplicate = await specializationRepository.AnyAsync(
				s => s.Name == request.Name,
				cancellationToken);

			if (isDuplicate)
			{
				return Result.Failure<Guid>(new Error(
					"Specialization.Duplicate",
					"هذا التخصص موجود مسبقاً في النظام."));
			}

			// 2. إنشاء التخصص (مع تمرير الوصف والأيقونة)
			var specialization = ContentCore.Domain.Entities.Specialization.Create(request.Name, request.Description, request.Icon);

			// 3. الحفظ بالداتابيز
			await specializationRepository.AddAsync(specialization, cancellationToken);
			await unitOfWork.SaveChangesAsync(cancellationToken);

			return Result.Success(specialization.Id);
		}
	}
}
