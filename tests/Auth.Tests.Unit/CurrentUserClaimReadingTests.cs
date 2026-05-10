using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using YallaJo.Api.Services;

namespace Auth.Tests.Unit;

/// <summary>
/// JWT-401 hardening regression tests for <see cref="CurrentUser"/>.
///
/// <para>
/// The Api-side <see cref="CurrentUser"/> reads JWT principal claims.  It must
/// be defensive across the known claim-type spellings so the API keeps working
/// regardless of whether <c>JwtBearerOptions.MapInboundClaims</c> is honoured
/// by the runtime: in some runtime configurations the option is silently
/// ignored and <c>JwtSecurityTokenHandler</c> auto-maps inbound <c>"sub"</c> to
/// <see cref="ClaimTypes.NameIdentifier"/> (and <c>"role"</c> to
/// <see cref="ClaimTypes.Role"/>).
/// </para>
///
/// <para>
/// These tests pin the defensive multi-claim reads added in
/// <c>YallaJo.Api/Services/CurrentUser.cs</c> so a future revert that drops
/// any one of the fallback paths fails loudly here instead of silently
/// breaking every endpoint that depends on <c>currentUser.UserId</c>.
/// </para>
/// </summary>
public sealed class CurrentUserClaimReadingTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CurrentUser BuildSubject(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "Bearer");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        return new CurrentUser(accessor);
    }

    private static CurrentUser BuildAnonymousSubject()
    {
        // Identity with no AuthenticationType → IsAuthenticated == false.
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        return new CurrentUser(accessor);
    }

    // ── UserId — every accepted claim spelling ────────────────────────────────

    [Fact]
    public void CurrentUser_ReadsUserId_FromSubClaim()
    {
        var userId = Guid.NewGuid();
        var subject = BuildSubject(new Claim("sub", userId.ToString()));

        subject.UserId.Should().Be(userId,
            "the canonical claim emitted by JwtTokenService is \"sub\" and must be the primary read");
    }

    [Fact]
    public void CurrentUser_ReadsUserId_FromNameIdentifierClaim()
    {
        // Simulates the runtime auto-mapping "sub" → ClaimTypes.NameIdentifier
        // when MapInboundClaims is silently honoured (or ignored).
        var userId = Guid.NewGuid();
        var subject = BuildSubject(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

        subject.UserId.Should().Be(userId,
            "ClaimTypes.NameIdentifier must be a fallback so a runtime that auto-maps " +
            "\"sub\" doesn't break every endpoint that depends on UserId");
    }

    [Fact]
    public void CurrentUser_ReadsUserId_FromNameIdClaim_IfSupported()
    {
        // Some IdP libraries emit the short "nameid" form.
        var userId = Guid.NewGuid();
        var subject = BuildSubject(new Claim("nameid", userId.ToString()));

        subject.UserId.Should().Be(userId,
            "the short \"nameid\" claim is a recognised user-id alias and must resolve");
    }

    [Fact]
    public void CurrentUser_PrefersSubOverNameIdentifier_WhenBothPresent()
    {
        // If a token were ever to carry both spellings (e.g. a re-issued or
        // proxied token), "sub" must win to match the canonical issuer contract.
        var sub = Guid.NewGuid();
        var nameIdentifier = Guid.NewGuid();
        var subject = BuildSubject(
            new Claim("sub", sub.ToString()),
            new Claim(ClaimTypes.NameIdentifier, nameIdentifier.ToString()));

        subject.UserId.Should().Be(sub,
            "\"sub\" is the canonical issuer claim and must take precedence over the runtime-mapped form");
    }

    [Fact]
    public void CurrentUser_ReturnsNullUserId_WhenNoValidGuidClaim()
    {
        var subject = BuildSubject(new Claim("sub", "not-a-guid"));

        subject.UserId.Should().BeNull(
            "a non-Guid value in any user-id claim must produce null, not throw");
    }

    [Fact]
    public void CurrentUser_ReturnsNullUserId_WhenNoUserIdClaimAtAll()
    {
        var subject = BuildSubject(new Claim("email", "user@example.com"));

        subject.UserId.Should().BeNull(
            "absence of every user-id claim spelling must produce null, not throw");
    }

    [Fact]
    public void CurrentUser_ReturnsNullUserId_WhenHttpContextIsNull()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.ReturnsNull();
        var subject = new CurrentUser(accessor);

        subject.UserId.Should().BeNull("a missing HttpContext must produce null safely");
    }

    // ── IsAuthenticated ──────────────────────────────────────────────────────

    [Fact]
    public void CurrentUser_IsAuthenticated_WhenIdentityHasAuthenticationType()
    {
        var subject = BuildSubject(new Claim("sub", Guid.NewGuid().ToString()));
        subject.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void CurrentUser_IsNotAuthenticated_WhenIdentityHasNoAuthenticationType()
    {
        var subject = BuildAnonymousSubject();
        subject.IsAuthenticated.Should().BeFalse();
    }

    // ── Roles ────────────────────────────────────────────────────────────────

    [Fact]
    public void CurrentUser_ReadsRoles_FromRoleClaim()
    {
        var subject = BuildSubject(
            new Claim("role", "Admin"),
            new Claim("role", "Editor"));

        subject.Roles.Should().BeEquivalentTo(new[] { "Admin", "Editor" },
            "the canonical claim emitted by JwtTokenService is \"role\"");
    }

    [Fact]
    public void CurrentUser_ReadsRoles_FromClaimTypesRole()
    {
        // Simulates runtime mapping "role" → ClaimTypes.Role.
        var subject = BuildSubject(new Claim(ClaimTypes.Role, "Admin"));

        subject.Roles.Should().Contain("Admin",
            "ClaimTypes.Role must be read so a runtime-mapped token still authorises");
    }

    [Fact]
    public void CurrentUser_ReadsRoles_FromBothRoleAndClaimTypesRole_DistinctCaseInsensitive()
    {
        // Mixed token: "role" and ClaimTypes.Role both present with same value.
        var subject = BuildSubject(
            new Claim("role", "Admin"),
            new Claim(ClaimTypes.Role, "admin"),
            new Claim("role", "Editor"));

        subject.Roles.Should().BeEquivalentTo(new[] { "Admin", "Editor" },
            "duplicate roles across the two spellings must collapse case-insensitively");
    }

    [Fact]
    public void CurrentUser_IsInRole_MatchesEitherSpelling()
    {
        var subject = BuildSubject(new Claim(ClaimTypes.Role, "SuperAdmin"));

        subject.IsInRole("SuperAdmin").Should().BeTrue();
        subject.IsInRole("superadmin").Should().BeTrue("role match is case-insensitive");
    }

    // ── Permissions ──────────────────────────────────────────────────────────

    [Fact]
    public void CurrentUser_ReadsPermissions_FromCapitalPermissionClaim()
    {
        // Token issuer emits capital-P "Permission" per AppClaimTypes.Permission.
        var subject = BuildSubject(
            new Claim("Permission", "Permission.Role.Read"),
            new Claim("Permission", "Permission.Role.Update"));

        subject.Permissions.Should().BeEquivalentTo(
            new[] { "Permission.Role.Read", "Permission.Role.Update" },
            "the JWT issuer emits the capital-P \"Permission\" claim type and the " +
            "API-side reader must include it");
    }

    [Fact]
    public void CurrentUser_ReadsPermissions_FromLowercasePermissionClaim()
    {
        // Backwards-compatibility with any token source that emits lowercase.
        var subject = BuildSubject(new Claim("permission", "Permission.Tour.Create"));

        subject.Permissions.Should().Contain("Permission.Tour.Create",
            "the lowercase \"permission\" claim type must remain readable for legacy/mixed tokens");
    }

    [Fact]
    public void CurrentUser_ReadsPermissions_FromBothCases_DistinctCaseInsensitive()
    {
        var subject = BuildSubject(
            new Claim("Permission", "Permission.Role.Read"),
            new Claim("permission", "permission.role.read"),
            new Claim("Permission", "Permission.Tour.Create"));

        subject.Permissions.Should().BeEquivalentTo(
            new[] { "Permission.Role.Read", "Permission.Tour.Create" },
            "duplicate permissions across the two spellings must collapse case-insensitively");
    }

    [Fact]
    public void CurrentUser_HasPermission_MatchesEitherSpelling()
    {
        var subject = BuildSubject(new Claim("Permission", "Permission.Role.Read"));

        subject.HasPermission("Permission.Role.Read").Should().BeTrue();
        subject.HasPermission("permission.role.read").Should().BeTrue("permission match is case-insensitive");
    }

    // ── Email / Name ─────────────────────────────────────────────────────────

    [Fact]
    public void CurrentUser_ReadsEmail_FromEmailClaim()
    {
        var subject = BuildSubject(new Claim("email", "user@example.com"));
        subject.Email.Should().Be("user@example.com");
    }

    [Fact]
    public void CurrentUser_ReadsEmail_FromClaimTypesEmail()
    {
        var subject = BuildSubject(new Claim(ClaimTypes.Email, "user@example.com"));
        subject.Email.Should().Be("user@example.com",
            "ClaimTypes.Email must be a fallback for runtime-mapped tokens");
    }
}
