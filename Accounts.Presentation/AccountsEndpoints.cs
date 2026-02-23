using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Accounts.Presentation;

public static class AccountsEndpoints
{
    public static IEndpointRouteBuilder MapAccountsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api/accounts");

        return endpoints;
    }
}
