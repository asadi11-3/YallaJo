using Accounts.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Queries.GetUserProfile
{
    public sealed record GetUserProfileQuery(Guid UserId) : IQuery<UserProfileDto>;
}
