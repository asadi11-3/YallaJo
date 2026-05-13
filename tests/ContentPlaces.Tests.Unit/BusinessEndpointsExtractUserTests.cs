using System.Security.Claims;
using ContentPlaces.Presentation.Endpoints.Business;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Security.Contracts.Authorization;

namespace ContentPlaces.Tests.Unit;

/// <summary>
/// CONTENTPLACES-FOLLOWUP-ADMIN-TIER-001 regression tests for
/// <see cref="BusinessEndpoints.ExtractUser"/>.
///
/// <para>
/// The previous implementation used <c>http.User.IsInRole("Admin")</c>, which
/// only matched the literal <c>"Admin"</c> role and silently treated
/// <c>SuperAdmin</c> and <c>Owner</c> as standard callers — scoping them down
/// on the public Business-detail read path.  The fix routes the admin-tier
/// check through the project-standard helper:
/// </para>
///
/// <code>
/// AppRoles.HighestPrivilegeLevel(roles) &gt;= RolePrivilegeLevel.Admin
/// </code>
///
/// <para>
/// These tests pin the five required behaviours so the helper cannot regress
/// to <c>IsInRole("Admin")</c> in the future.
/// </para>
/// </summary>
public sealed class BusinessEndpointsExtractUserTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static HttpContext BuildContext(Guid? userId, params string[] roles)
    {
        var claims = new List<Claim>();

        if (userId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Authenticated when at least one claim is present; unauthenticated
        // (anonymous) when claims is empty — matches ASP.NET Core defaults.
        var identity = claims.Count > 0
            ? new ClaimsIdentity(claims, authenticationType: "TestAuth")
            : new ClaimsIdentity(); // anonymous principal

        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity),
        };
    }

    // ── 1. Admin ──────────────────────────────────────────────────────────────

    [Fact]
    public void ExtractUser_AdminRole_IsAdminTrue()
    {
        var http = BuildContext(Guid.NewGuid(), AppRoles.Admin);

        var (_, isAdmin) = BusinessEndpoints.ExtractUser(http);

        isAdmin.Should().BeTrue(
            "Admin role must be recognised as admin-tier (RolePrivilegeLevel.Admin == 60)");
    }

    [Fact]
    public void ExtractUser_SuperAdminRole_IsAdminTrue()
    {
        var http = BuildContext(Guid.NewGuid(), AppRoles.SuperAdmin);

        var (_, isAdmin) = BusinessEndpoints.ExtractUser(http);

        isAdmin.Should().BeTrue(
            "SuperAdmin (privilege 80) must qualify as admin-tier (>= Admin); the previous " +
            "IsInRole(\"Admin\") check silently treated SuperAdmin as a standard caller.");
    }

    // ── 3. Owner ──────────────────────────────────────────────────────────────

    [Fact]
    public void ExtractUser_OwnerRole_IsAdminTrue()
    {
        var http = BuildContext(Guid.NewGuid(), AppRoles.Owner);

        var (_, isAdmin) = BusinessEndpoints.ExtractUser(http);

        isAdmin.Should().BeTrue(
            "Owner (privilege 100) must qualify as admin-tier (>= Admin); the previous " +
            "IsInRole(\"Admin\") check silently treated Owner as a standard caller.");
    }

    // ── 4. Normal authenticated user (no admin-tier role) ────────────────────

    [Fact]
    public void ExtractUser_NormalUser_IsAdminFalse()
    {
        var http = BuildContext(Guid.NewGuid(), AppRoles.User);

        var (userId, isAdmin) = BusinessEndpoints.ExtractUser(http);

        userId.Should().NotBeNull("authenticated user should round-trip its UserId claim");
        isAdmin.Should().BeFalse(
            "User role (privilege 10) is below Admin tier and must not be flagged as admin");
    }

    // ── 5. Anonymous (no identity claims) ────────────────────────────────────

    [Fact]
    public void ExtractUser_Anonymous_IsAdminFalse()
    {
        var http = BuildContext(userId: null);

        var (userId, isAdmin) = BusinessEndpoints.ExtractUser(http);

        userId.Should().BeNull("anonymous callers have no NameIdentifier claim");
        isAdmin.Should().BeFalse(
            "anonymous callers carry no role claims; HighestPrivilegeLevel returns None < Admin");
    }

    // ── 6. Bonus regression guard: TourGuide is not admin-tier ───────────────

    /// <summary>
    /// Extra guard to ensure the helper does not silently elevate any
    /// non-admin role.  TourGuide is a standard role (privilege 10).
    /// </summary>
    [Fact]
    public void ExtractUser_TourGuideRole_IsAdminFalse()
    {
        var http = BuildContext(Guid.NewGuid(), AppRoles.TourGuide);

        var (_, isAdmin) = BusinessEndpoints.ExtractUser(http);

        isAdmin.Should().BeFalse(
            "TourGuide (Standard tier) must not qualify as admin-tier");
    }
}
