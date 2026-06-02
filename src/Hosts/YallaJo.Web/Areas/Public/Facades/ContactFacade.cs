using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Models.Contact;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Public.Facades;

public sealed class ContactFacade
{
    private readonly ContactApiClient _api;

    public ContactFacade(ContactApiClient api) => _api = api;

    public async Task<ApiResult<Guid>> SubmitAsync(ContactFormVm form, CancellationToken ct = default)
    {
        var request = new CreateTicketRequest(
            Category: string.IsNullOrWhiteSpace(form.Category) ? "Other" : form.Category.Trim(),
            Subject: form.Subject.Trim(),
            Body: form.Message.Trim());

        var result = await _api.SubmitTicketAsync(request, ct);

        if (result.IsSuccess)
            return ApiResult<Guid>.Ok(result.Data, result.StatusCode);

        if (result.IsUnauthorized)
            return ApiResult<Guid>.ForceSignOut();

        if (result.IsValidationError && result.ValidationErrors is not null)
            return ApiResult<Guid>.ValidationFail(result.StatusCode, result.ValidationErrors);

        return ApiResult<Guid>.Fail(result.StatusCode, result.Error ?? "Your message could not be sent. Please try again.");
    }
}
