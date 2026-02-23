using Accounts.Application.DTOs;
using Accounts.Application.Specifications;
using Accounts.Domain.Entities;
using Accounts.Domain.Interfaces;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Queries.GetUserProfile
{
  
        public sealed class GetUserProfileQueryHandler : IQueryHandler<GetUserProfileQuery, UserProfileDto>
        {
            private readonly IUserRepository _userRepository;

            public GetUserProfileQueryHandler(IUserRepository userRepository)
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


