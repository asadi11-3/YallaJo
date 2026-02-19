using Accounts.Domain.Entities;
using YallaJo.SharedKernel.Application.Abstractions.Specifications;

namespace Accounts.Application.Specifications
{
    public sealed class UserWithEmailsSpecification : Specification<User>
    {
        public UserWithEmailsSpecification(Guid userId)
        {
            Where(u => u.Id == userId);
            Include(u => u.Emails);
        }
    }
}
