using Microsoft.AspNetCore.Identity;
using Security.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Security.Infrastructure.Persistence
{
    internal sealed class PasswordHasher : IPasswordHasher
    {
        private readonly PasswordHasher<object> _hasher = new();

        public string Hash(string password)
            => _hasher.HashPassword(null!, password);

        public bool Verify(string password, string passwordHash)
            => _hasher.VerifyHashedPassword(null!, passwordHash, password)
               is PasswordVerificationResult.Success
               or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
