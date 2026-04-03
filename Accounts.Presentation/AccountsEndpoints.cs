using Accounts.Presentation.Endpoints.Profile;
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

        return endpoints;
    }
}
