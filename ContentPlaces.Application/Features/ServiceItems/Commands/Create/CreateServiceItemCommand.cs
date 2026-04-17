using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentPlaces.Application.Features.ServiceItems.Commands.Create
{
    // الـ Command رح يرجع Guid (اللي هو الـ ID تبع الخدمة اللي انضافت)
    public sealed record CreateServiceItemCommand(
        Guid BusinessId,
        string Name,
        decimal Price,
        int DurationMinutes,
        int MaxCapacity,
        string Currency,
        int SortOrder
    ) : IRequest<Guid>; // أو ICommand<Guid> حسب شو بتستخدموا بالمشروع
}
