using ContentCore.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ContentCore.Domain.Entities;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Data;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using ContentCore.Application.Commands.Tag.CreateTag;

namespace ContentCore.Application.Commands.Tags.CreateTag
{
	public sealed class CreateTagCommandHandler(ITagRepository tagRepository,
		IContentCoreUnitOfWork unitofwork) : ICommandHandler<CreateTagCommand, CreateTagResult>
	{
		public async Task<Result<CreateTagResult>> Handle(CreateTagCommand request, CancellationToken ct) 
		{
			// -------------------------------------------------------------
			//		// 1. اللوجيك: فحص البزنس (هل التاغ موجود أصلاً؟)
			//		// -------------------------------------------------------------
			//		// بنشيك إذا في تاغ بنفس الاسم أو الـ Slug
			bool isDuplicate = await tagRepository.AnyAsync(
				t => t.Name == request.Name || t.Slug == request.Slug,
				ct);

					if (isDuplicate)
					{
			//			// إذا موجود، بنوقف الشغل وبنرجع إيرور للبزنس
						return Result.Failure<CreateTagResult>(new Error(
							"Tag.Duplicate",
							"هذا التخصص أو التاغ موجود مسبقاً في النظام."));
					}

					// -------------------------------------------------------------
					// 2. اللوجيك: إنشاء الكيان (بناءً على طلبات البزنس)
					// -------------------------------------------------------------
					var tag = ContentCore.Domain.Entities.Tag.Create(request.Name, request.Slug);
			// (إذا في Domain Event بنضيفه هون أو جوا الـ Create زي ما شرح الدكتور)

			//		// -------------------------------------------------------------
			//		// 3 & 4. اللوجيك: الحفظ في قاعدة البيانات
			//		// -------------------------------------------------------------
			await tagRepository.AddAsync(tag, ct);
			await unitofwork.SaveChangesAsync(ct); // هون بتتسيف الداتا + بنبعت الـ Events

			return Result<CreateTagResult>.Created(new CreateTagResult(tag.Id,tag.Name,tag.Slug));
		}
	}



}
