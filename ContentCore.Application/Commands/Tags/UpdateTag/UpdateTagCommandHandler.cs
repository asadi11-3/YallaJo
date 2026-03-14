using ContentCore.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Data;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Tags.UpdateTag;
internal sealed class UpdateTagCommandHandler(
	ITagRepository tagRepository,
	YallaJo.SharedKernel.Domain.Abstractions.Data.IUnitOfWork unitOfWork) : ICommandHandler<UpdateTagCommand, Guid>
{
	public async Task<Result<Guid>> Handle(UpdateTagCommand request, CancellationToken ct)
	{
		// 1. ندور على التاغ في الداتابيز عن طريق الـ Id
		var tag = await tagRepository.GetByIdAsync(request.Id, ct); // تأكد من اسم الميثود عندك

		// إذا ما لقيناه، بنرجع إيرور 404
		if (tag is null)
		{
			return Result.Failure<Guid>(new Error("Tag.NotFound", "التاغ غير موجود في النظام."));
		}

		// 2. فحص البزنس: نتأكد إنه الاسم الجديد أو الـ Slug مش مستخدم لتاغ "ثاني"
		bool isDuplicate = await tagRepository.AnyAsync(
			t => (t.Name == request.Name || t.Slug == request.Slug) && t.Id != request.Id,
			ct);

		if (isDuplicate)
		{
			return Result.Failure<Guid>(new Error("Tag.Duplicate", "الاسم أو الرابط مستخدم لتاغ آخر."));
		}

		// 3. التعديل الفعلي باستخدام الميثود اللي ضفناها بالدومين
		tag.Update(request.Name, request.Slug,request.IsActive);

		// 4. الحفظ (Commit)
		// ملاحظة: مع الـ Entity Framework، إنت مش محتاج تعمل Update، بس بتعمل Save للـ UnitOfWork وهو بيكتشف التغيير لحاله
		await unitOfWork.SaveChangesAsync(ct);

		return Result.Success(tag.Id);
	} 
}



