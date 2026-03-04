using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Security.Contracts.Authorization
{
    public static class AppFeatures
    {
        public const string Role = nameof(Role);
        public const string UserRole = nameof(UserRole);
        public const string RoleClaim = nameof(RoleClaim);
        public const string System = nameof(System);
        public const string User = nameof(User);
    }
}
