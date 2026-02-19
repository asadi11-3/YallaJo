using Accounts.Application.DTOs;
using Accounts.Application.Specifications;
using Accounts.Domain.Entities;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Results;

namespace Accounts.Application.Queries.GetUserProfile
{
  
        public sealed class GetUserProfileQueryHandler : IQueryHandler<GetUserProfileQuery, UserProfileDto>
        {
            private readonly IReadRepository<User,Guid> _userRepository;

            public GetUserProfileQueryHandler(IReadRepository<User, Guid> userRepository)
            {
                _userRepository = userRepository;
            }

            public async Task<Result<UserProfileDto>> Handle(
                GetUserProfileQuery request,
                CancellationToken cancellationToken)
            {
              
                var spec = new UserProfileSpecification(request.UserId);
                var users = await _userRepository.ListAsync(spec, cancellationToken);
                var userDto = users.FirstOrDefault();

                if (userDto is null)
                    return Error.NotFound("User", "User not found");

                return Result<UserProfileDto>.Success(userDto);
            }
        }
    }


