using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Security.Contracts.Authorization
{
    public static class AppRoles
    {
        public const string Admin = nameof(Admin);
        public const string SuperAdmin = nameof(SuperAdmin);
        public const string Owner = nameof(Owner);
        public const string User = nameof(User);
        public const string TourGuide = nameof(TourGuide);
        public const string Guest = nameof(Guest);

        public static IReadOnlyList<string> AllRoles { get; } = new[]
            { SuperAdmin, Admin, Owner, User, TourGuide, Guest };

        public static IReadOnlyList<string> DefaultRoles { get; } = new[] { User };

        public static IReadOnlyList<string> ProtectedRoles { get; } = new[] { SuperAdmin, Owner };

        public static IReadOnlyList<string> OwnerOnlyRoles { get; } = new[] { Owner, SuperAdmin };

        public static bool IsValidRole(string role) =>
            AllRoles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
    }

}
