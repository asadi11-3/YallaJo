using Accounts.Application.DTOs;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Queries.GetUserProfile
{
    public sealed record GetUserProfileQuery(Guid UserId) : IQuery<UserProfileDto>;
}
