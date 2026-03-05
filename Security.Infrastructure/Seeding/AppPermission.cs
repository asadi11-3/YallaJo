using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Security.Infrastructure.Seeding
{
    public record AppPermission(string Feature, string Action, string Group, string Description, bool IsGuest = false)
    {
        public string Name => NameFor(Feature, Action);

        public static string NameFor(string feature, string action) =>
            $"Permission.{feature}.{action}";
    }
}
