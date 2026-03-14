using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Tags.UpdateTag
{
	// بنطلب الـ Id، والاسم الجديد، والـ Slug الجديد
	public sealed record UpdateTagCommand(Guid Id, string Name, string Slug , bool IsActive) : ICommand<Guid>;
}
