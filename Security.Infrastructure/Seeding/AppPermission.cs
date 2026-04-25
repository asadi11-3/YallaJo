using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Security.Infrastructure.Seeding
{
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
    public record AppPermission(string Feature, string Action, string Group, string Description, bool IsGuest = false)
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
    {
        public string Name => NameFor(Feature, Action);

        public static string NameFor(string feature, string action) =>
            $"Permission.{feature}.{action}";
    }
}
