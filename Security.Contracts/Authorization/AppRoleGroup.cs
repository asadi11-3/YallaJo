using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Security.Contracts.Authorization
{
    public static class AppRoleGroup
    {
        public const string SystemAccess = nameof(SystemAccess);
        public const string ContentManagement = nameof(ContentManagement);
    }
}
