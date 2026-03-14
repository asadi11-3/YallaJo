using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentCore.Application.Commands.Specialization.CreateTag
{
	public sealed class CreateSpecializationCommandValidator : AbstractValidator<CreateSpecializationCommand>
	{
		public CreateSpecializationCommandValidator()
		{
			// الاسم إجباري وممنوع يكون فاضي
			RuleFor(x => x.Name)
				.NotEmpty().WithMessage("اسم التخصص مطلوب.")
				.MaximumLength(100);

			// الوصف والأيقونة اختياريات، بس إذا انبعثوا بنحدد طولهم الأقصى
			RuleFor(x => x.Description)
				.MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Description));

			RuleFor(x => x.Icon)
				.MaximumLength(255).When(x => !string.IsNullOrEmpty(x.Icon));
		}
	}
}
