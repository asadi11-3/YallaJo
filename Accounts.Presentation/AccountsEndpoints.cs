using Accounts.Presentation.Endpoints.Profile;
using Accounts.Presentation.Endpoints.Provider;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Accounts.Presentation;

public static class AccountsEndpoints
{
    public static IEndpointRouteBuilder MapAccountsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/accounts")
            .WithTags("Accounts");

        ProfileEndpoints.MapProfileEndpoints(group);

        // Provider application endpoints (user-facing)
        var providerGroup = endpoints.MapGroup("/api/v1/provider")
            .WithTags("Provider");

        ProviderEndpoints.MapProviderEndpoints(providerGroup);

        // Admin provider queue endpoints
        var adminProviderGroup = endpoints.MapGroup("/api/v1/admin/providers")
            .WithTags("Admin - Providers");

        AdminProviderEndpoints.MapAdminProviderEndpoints(adminProviderGroup);

        return endpoints;
    }
}
