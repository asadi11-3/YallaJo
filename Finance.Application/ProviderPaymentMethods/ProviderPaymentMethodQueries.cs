using Finance.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.ProviderPaymentMethods;

public sealed record GetProviderPaymentMethodsQuery(Guid UserId) : IRequest<Result<IReadOnlyList<ProviderPaymentMethodDto>>>;

public sealed class GetProviderPaymentMethodsQueryHandler(
    IProviderPaymentMethodRepository repository,
    ILogger<GetProviderPaymentMethodsQueryHandler> logger)
    : IRequestHandler<GetProviderPaymentMethodsQuery, Result<IReadOnlyList<ProviderPaymentMethodDto>>>
{
    public async Task<Result<IReadOnlyList<ProviderPaymentMethodDto>>> Handle(GetProviderPaymentMethodsQuery request, CancellationToken ct)
    {
        var methods = await repository.GetByUserIdAsync(request.UserId, ct);
        logger.LogDebug("Read {Count} provider payment methods for provider {ProviderId}.", methods.Count, request.UserId);
        return Result.Success<IReadOnlyList<ProviderPaymentMethodDto>>(methods.Select(ProviderPaymentMethodMapper.ToDto).ToList());
    }
}
