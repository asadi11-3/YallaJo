using ContentCore.Application.Commands.Tags.UpdateTag;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentCore.Application.Commands.Specialization.UpdateSpecialization
{
	public sealed class UpdateSpecializationCommandValidator : AbstractValidator<UpdateSpecializationCommand>
	{
		public UpdateSpecializationCommandValidator()
		{
			// الاسم إجباري
			RuleFor(x => x.Name)
				.NotEmpty().WithMessage("اسم التخصص مطلوب.")
				.MaximumLength(100);

			// الوصف والأيقونة اختياريات، بس إذا انبعثوا بنحدد طولهم
			RuleFor(x => x.Description)
				.MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Description));

			RuleFor(x => x.Icon)
				.MaximumLength(255).When(x => !string.IsNullOrEmpty(x.Icon));
		}
	}

}
