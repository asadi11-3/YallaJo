using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace YallaJo.Authorization.IntegrationTests;

/// <summary>
/// Sanity tests enforcing the 5 non-negotiable authorization rules from agent-context.md §0.3.
///
/// These tests are EXPECTED to be RED during Phase 3 Auth-Cleanup pre-work and turn GREEN
/// once the sprint team has eliminated all violations catalogued in
/// <c>Agents/endpoint-violations-2027-02-28.csv</c>.
///
/// Categorised under <c>[Trait("Category", "authorization-debt")]</c> so they can be excluded
/// from the default CI run until the cleanup sprint ships.
/// </summary>
[Trait("Category", "authorization-debt")]
public sealed class AuthorizationSanityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthorizationSanityTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Every endpoint MUST have either <see cref="MustHavePermissionAttribute"/>
    /// or <see cref="AllowAnonymousAttribute"/> in its metadata.
    /// Bare <c>.RequireAuthorization()</c> is forbidden.
    /// </summary>
    [Fact]
    public void Every_endpoint_must_have_MustHavePermission_or_AllowAnonymous()
    {
        var endpointDataSource = _factory.Services.GetRequiredService<EndpointDataSource>();
        var offenders = new List<string>();

        foreach (var endpoint in endpointDataSource.Endpoints)
        {
            var displayName = endpoint.DisplayName ?? "<unknown>";

            var hasPermission = endpoint.Metadata.GetMetadata<MustHavePermissionAttribute>() is not null;
            var hasAllowAnonymous = endpoint.Metadata.GetMetadata<AllowAnonymousAttribute>() is not null;

            if (!hasPermission && !hasAllowAnonymous)
            {
                offenders.Add(displayName);
            }
        }

        offenders.Should().BeEmpty(
            "every endpoint must declare MustHavePermissionAttribute or AllowAnonymousAttribute; " +
            $"{offenders.Count} endpoint(s) violated this rule");
    }

    /// <summary>
    /// String-based policy names like <c>.RequireAuthorization("Admin")</c> are forbidden.
    /// All authorization decisions must flow through <see cref="MustHavePermissionAttribute"/>.
    /// </summary>
    [Fact]
    public void No_endpoint_may_use_string_policy_authorization()
    {
        var endpointDataSource = _factory.Services.GetRequiredService<EndpointDataSource>();
        var offenders = new List<string>();

        foreach (var endpoint in endpointDataSource.Endpoints)
        {
            var authzData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
            foreach (var data in authzData)
            {
                if (!string.IsNullOrWhiteSpace(data.Policy))
                {
                    offenders.Add($"{endpoint.DisplayName} -> policy '{data.Policy}'");
                }
            }
        }

        offenders.Should().BeEmpty(
            "no endpoint may attach a string-based authorization policy; " +
            $"{offenders.Count} endpoint(s) violated this rule. Replace with MustHavePermissionAttribute.");
    }

    /// <summary>
    /// Every <see cref="MustHavePermissionAttribute"/> on an endpoint must reference
    /// a permission registered in some loaded <see cref="IPermissionCatalog"/>.
    /// This catches typos and stale references.
    /// </summary>
    [Fact]
    public void Every_MustHavePermission_must_reference_a_registered_permission()
    {
        var endpointDataSource = _factory.Services.GetRequiredService<EndpointDataSource>();
        var catalogs = _factory.Services.GetServices<IPermissionCatalog>();

        var registered = new HashSet<(string Feature, string Action)>(
            catalogs.SelectMany(c => c.Permissions).Select(p => (p.Feature, p.Action)));

        var offenders = new List<string>();

        foreach (var endpoint in endpointDataSource.Endpoints)
        {
            var attrs = endpoint.Metadata.GetOrderedMetadata<MustHavePermissionAttribute>();
            foreach (var attr in attrs)
            {
                if (!registered.Contains((attr.Feature, attr.Action)))
                {
                    offenders.Add($"{endpoint.DisplayName} -> {attr.Feature}.{attr.Action}");
                }
            }
        }

        offenders.Should().BeEmpty(
            "every MustHavePermissionAttribute must reference a (Feature, Action) pair " +
            $"registered in some IPermissionCatalog; {offenders.Count} endpoint(s) violated this rule");
    }
}
