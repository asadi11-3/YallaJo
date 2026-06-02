using YallaJo.Web.Areas.Public.Models.Contact;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.ApiClients;

public sealed class ContactApiClient
{
    private readonly IApiClient _api;

    public ContactApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<Guid>> SubmitTicketAsync(CreateTicketRequest request, CancellationToken ct = default)
        => _api.PostAsync<Guid>("/api/v1/support/tickets", request, ct);
}
