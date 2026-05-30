using Finance.Application.Interfaces;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.ProviderPaymentMethods;

public sealed record CreateProviderPaymentMethodCommand(
    Guid UserId,
    ProviderPaymentMethodType PaymentMethodType,
    string DisplayName,
    string AccountIdentifier,
    string? BankName,
    bool IsDefault) : IRequest<Result<ProviderPaymentMethodDto>>;

public sealed record UpdateProviderPaymentMethodCommand(
    Guid Id,
    Guid UserId,
    ProviderPaymentMethodType PaymentMethodType,
    string DisplayName,
    string AccountIdentifier,
    string? BankName,
    bool IsDefault) : IRequest<Result<ProviderPaymentMethodDto>>;

public sealed record DeleteProviderPaymentMethodCommand(Guid Id, Guid UserId) : IRequest<Result<bool>>;

public sealed record VerifyProviderPaymentMethodCommand(Guid Id, Guid AdminId, bool IsVerified) : IRequest<Result<ProviderPaymentMethodDto>>;

public sealed class CreateProviderPaymentMethodCommandHandler(
    IProviderPaymentMethodRepository repository,
    IFinanceUnitOfWork unitOfWork,
    ILogger<CreateProviderPaymentMethodCommandHandler> logger)
    : IRequestHandler<CreateProviderPaymentMethodCommand, Result<ProviderPaymentMethodDto>>
{
    public async Task<Result<ProviderPaymentMethodDto>> Handle(CreateProviderPaymentMethodCommand request, CancellationToken ct)
    {
        if (await repository.ExistsForProviderAsync(request.UserId, request.AccountIdentifier, ct))
        {
            return Result.Failure<ProviderPaymentMethodDto>(
                new Error("ProviderPaymentMethod.AlreadyExists", "Payment method already exists for this provider."),
                Outcome.Conflict);
        }

        var createResult = ProviderPaymentMethod.Create(
            request.UserId,
            request.PaymentMethodType,
            request.DisplayName,
            request.AccountIdentifier,
            request.BankName,
            request.IsDefault);

        if (createResult.IsFailure)
        {
            return Result.Failure<ProviderPaymentMethodDto>(createResult.Errors[0], createResult.Outcome);
        }

        var method = createResult.Value!;
        if (request.IsDefault)
        {
            await ClearDefaultMethodsAsync(request.UserId, repository, ct);
        }

        await repository.AddAsync(method, ct);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Created provider payment method {MethodId} for provider {ProviderId}.", method.Id, method.UserId);
        return Result.Success(ProviderPaymentMethodMapper.ToDto(method));
    }

    private static async Task ClearDefaultMethodsAsync(Guid userId, IProviderPaymentMethodRepository repository, CancellationToken ct)
    {
        var methods = await repository.GetByUserIdTrackedAsync(userId, ct);
        foreach (var method in methods.Where(x => x.IsDefault))
        {
            method.ClearDefault();
        }
    }
}

public sealed class UpdateProviderPaymentMethodCommandHandler(
    IProviderPaymentMethodRepository repository,
    IFinanceUnitOfWork unitOfWork,
    ILogger<UpdateProviderPaymentMethodCommandHandler> logger)
    : IRequestHandler<UpdateProviderPaymentMethodCommand, Result<ProviderPaymentMethodDto>>
{
    public async Task<Result<ProviderPaymentMethodDto>> Handle(UpdateProviderPaymentMethodCommand request, CancellationToken ct)
    {
        var method = await repository.GetByIdAsync(request.Id, ct);
        if (method is null)
        {
            return Result.Failure<ProviderPaymentMethodDto>(new Error("ProviderPaymentMethod.NotFound", "Payment method not found."), Outcome.NotFound);
        }

        if (method.UserId != request.UserId)
        {
            return Result.Failure<ProviderPaymentMethodDto>(new Error("ProviderPaymentMethod.Forbidden", "Payment method belongs to another provider."), Outcome.Forbidden);
        }

        var updateResult = method.Update(request.PaymentMethodType, request.DisplayName, request.AccountIdentifier, request.BankName);
        if (updateResult.IsFailure)
        {
            return Result.Failure<ProviderPaymentMethodDto>(updateResult.Errors[0], updateResult.Outcome);
        }

        if (request.IsDefault)
        {
            var methods = await repository.GetByUserIdTrackedAsync(request.UserId, ct);
            foreach (var other in methods.Where(x => x.Id != method.Id && x.IsDefault))
            {
                other.ClearDefault();
            }

            method.SetAsDefault();
        }

        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Updated provider payment method {MethodId}.", method.Id);
        return Result.Success(ProviderPaymentMethodMapper.ToDto(method));
    }
}

public sealed class DeleteProviderPaymentMethodCommandHandler(
    IProviderPaymentMethodRepository repository,
    IFinanceUnitOfWork unitOfWork,
    ILogger<DeleteProviderPaymentMethodCommandHandler> logger)
    : IRequestHandler<DeleteProviderPaymentMethodCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteProviderPaymentMethodCommand request, CancellationToken ct)
    {
        var method = await repository.GetByIdAsync(request.Id, ct);
        if (method is null)
        {
            return Result.Failure<bool>(new Error("ProviderPaymentMethod.NotFound", "Payment method not found."), Outcome.NotFound);
        }

        if (method.UserId != request.UserId)
        {
            return Result.Failure<bool>(new Error("ProviderPaymentMethod.Forbidden", "Payment method belongs to another provider."), Outcome.Forbidden);
        }

        method.SoftDelete();
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Deleted provider payment method {MethodId}.", method.Id);
        return Result.Success(true);
    }
}

public sealed class VerifyProviderPaymentMethodCommandHandler(
    IProviderPaymentMethodRepository repository,
    IFinanceUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<VerifyProviderPaymentMethodCommandHandler> logger)
    : IRequestHandler<VerifyProviderPaymentMethodCommand, Result<ProviderPaymentMethodDto>>
{
    public async Task<Result<ProviderPaymentMethodDto>> Handle(VerifyProviderPaymentMethodCommand request, CancellationToken ct)
    {
        var method = await repository.GetByIdAsync(request.Id, ct);
        if (method is null)
        {
            return Result.Failure<ProviderPaymentMethodDto>(new Error("ProviderPaymentMethod.NotFound", "Payment method not found."), Outcome.NotFound);
        }

        var result = request.IsVerified
            ? method.Verify(request.AdminId, timeProvider.GetUtcNow().UtcDateTime)
            : method.Unverify();

        if (result.IsFailure)
        {
            return Result.Failure<ProviderPaymentMethodDto>(result.Errors[0], result.Outcome);
        }

        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Verification changed for provider payment method {MethodId}.", method.Id);
        return Result.Success(ProviderPaymentMethodMapper.ToDto(method));
    }
}

internal static class ProviderPaymentMethodMapper
{
    public static ProviderPaymentMethodDto ToDto(ProviderPaymentMethod method)
        => new(
            method.Id,
            method.UserId,
            method.PaymentMethodType,
            method.IsDefault,
            method.IsVerified,
            method.DisplayName,
            method.AccountIdentifier,
            method.BankName,
            method.VerifiedAt,
            method.VerifiedByAdminId);
}
