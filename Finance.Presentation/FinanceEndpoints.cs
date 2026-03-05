using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Finance.Presentation;

public static class FinanceEndpoints
{
    public static IEndpointRouteBuilder MapFinanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
