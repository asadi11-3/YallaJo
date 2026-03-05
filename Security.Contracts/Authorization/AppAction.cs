using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Security.Contracts.Authorization
{
    public static class AppAction
    {
        public const string Read = nameof(Read);
        public const string Create = nameof(Create);
        public const string UpdateAny = nameof(UpdateAny);
        public const string DeleteAny = nameof(DeleteAny);
        public const string Update = nameof(Update);
        public const string Delete = nameof(Delete);
        public const string UpdateSelf = nameof(UpdateSelf);
        public const string SoftDelete = nameof(SoftDelete);
    }
}
