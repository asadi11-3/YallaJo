using System.Reflection;
using Security.Domain.Entities;

namespace Security.Tests.Unit.TestFixtures;

/// <summary>
/// Builds fully-populated <see cref="User"/> aggregates for unit tests. Uses
/// reflection to seed the private backing fields because the domain intentionally
/// hides role/claim mutation behind factory/business methods — that's safer in
/// production code but inconvenient for isolated handler tests.
/// </summary>
internal static class TestUserBuilder
{
    public static User CreateUserWithRoles(Guid userId, params string[] roleNames)
    {
        var user = (User)Activator.CreateInstance(typeof(User), nonPublic: true)!;

        SetPrivateProperty(user, nameof(User.Id), userId);
        SetPrivateProperty(user, nameof(User.IsActive), true);
        // Phase 2A: keep both representations consistent. IsActive remains
        // the legacy boolean; LifecycleState is the new source of truth.
        SetPrivateProperty(user, nameof(User.LifecycleState), AccountLifecycleState.Active);

        var userRolesField = typeof(User).GetField(
            "_userRoles",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var userRoles = (List<UserRole>)userRolesField.GetValue(user)!;

        foreach (var roleName in roleNames)
        {
            var role = Role.Create(roleName);
            var userRole = UserRole.Create(userId, role.Id);
            SetPrivateProperty(userRole, nameof(UserRole.Role), role);
            userRoles.Add(userRole);
        }

        return user;
    }

    private static void SetPrivateProperty(object target, string propertyName, object? value)
    {
        var prop = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        prop.GetSetMethod(nonPublic: true)!.Invoke(target, new[] { value });
    }
}
