using Accounts.Application.DTOs;
using Accounts.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Specifications;

namespace Accounts.Application.Specifications
{
    public sealed class UserProfileSpecification : Specification<User, UserProfileDto>
    {
        public UserProfileSpecification(Guid userId)
        {
         
            Where(u => u.Id == userId);           
            Include(u => u.Emails);        
            Select(u => new UserProfileDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive,
                Emails = u.Emails.Select(e => new UserEmailDto
                {
                    Id = e.Id,
                    Email = e.Address.Value,
                    IsPrimary = e.IsPrimary,
                    IsVerified = e.IsVerified,
                    VerifiedAt = e.VerifiedAt
                }).ToList()
            });
        }
    }
}
