using System;
using System.Collections.Generic;
using System.Linq;

namespace Security.Contracts.Authorization
{
    /// <summary>
    /// Ordered privilege tiers used to enforce role-management hierarchy.
    /// Higher numeric value = higher privilege.
    /// </summary>
    public enum RolePrivilegeLevel
    {
        /// <summary>Unknown or non-privileged/unrecognized role.</summary>
        None = 0,

        /// <summary>Standard business roles: User, TourGuide, Guest.</summary>
        Standard = 10,

        /// <summary>Admin — below SuperAdmin.</summary>
        Admin = 60,

        /// <summary>SuperAdmin — below Owner.</summary>
        SuperAdmin = 80,

        /// <summary>Owner — highest; singleton.</summary>
        Owner = 100,
    }

    public static class AppRoles
    {
        public const string Admin = nameof(Admin);
        public const string SuperAdmin = nameof(SuperAdmin);
        public const string Owner = nameof(Owner);
        public const string User = nameof(User);
        public const string Provider = nameof(Provider);
        public const string TourGuide = nameof(TourGuide);
        public const string Creator = nameof(Creator);
        public const string Guest = nameof(Guest);

        public static IReadOnlyList<string> AllRoles { get; } = new[]
            { SuperAdmin, Admin, Owner, Provider, TourGuide, Creator, User, Guest };

        public static IReadOnlyList<string> DefaultRoles { get; } = new[] { Guest };

        public static IReadOnlyList<string> ProtectedRoles { get; } = new[] { SuperAdmin, Owner };

        public static IReadOnlyList<string> OwnerOnlyRoles { get; } = new[] { Owner, SuperAdmin };

        public static bool IsValidRole(string role) =>
            AllRoles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));

        public static RolePrivilegeLevel GetPrivilegeLevel(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {

                return RolePrivilegeLevel.None;
            }

            if (roleName.Equals(Owner, StringComparison.OrdinalIgnoreCase))
            {
                return RolePrivilegeLevel.Owner;
            }

            if (roleName.Equals(SuperAdmin, StringComparison.OrdinalIgnoreCase))
            {
                return RolePrivilegeLevel.SuperAdmin;
            }

            if (roleName.Equals(Admin, StringComparison.OrdinalIgnoreCase))
            {
                return RolePrivilegeLevel.Admin;
            }

            if (roleName.Equals(User, StringComparison.OrdinalIgnoreCase)
                || roleName.Equals(TourGuide, StringComparison.OrdinalIgnoreCase)
                || roleName.Equals(Provider, StringComparison.OrdinalIgnoreCase)
                || roleName.Equals(Creator, StringComparison.OrdinalIgnoreCase)
                || roleName.Equals(Guest, StringComparison.OrdinalIgnoreCase))
            {
                return RolePrivilegeLevel.Standard;
            }
 
            return RolePrivilegeLevel.Standard;
        }

        public static RolePrivilegeLevel HighestPrivilegeLevel(IEnumerable<string>? roleNames)
        {
            if (roleNames is null)
            {
                return RolePrivilegeLevel.None;
            }

            var highest = RolePrivilegeLevel.None;
            foreach (var name in roleNames)
            {
                var level = GetPrivilegeLevel(name);
                if (level > highest)
                {
                    highest = level;
                }
            }

            return highest;
        }
    }
}

