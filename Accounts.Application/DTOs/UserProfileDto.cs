using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accounts.Application.DTOs
{
    public sealed record UserProfileDto
    {
        public Guid Id { get; init; }
        public string FirstName { get; init; }
        public string LastName { get; init; }
        public bool IsActive { get; init; }
        public List<UserEmailDto> Emails { get; init; } = new();
     
    }
}
