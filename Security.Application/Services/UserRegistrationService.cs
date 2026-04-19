using Microsoft.Extensions.Caching.Hybrid;
using Security.Application.Caching;
using Security.Application.Helpers;
using Security.Application.Interfaces;
using Security.Contracts.Abstractions;
using Security.Domain.Entities;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Services;

internal sealed class UserRegistrationService(
    IUserRepository userRepository,
    ISecurityUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    HybridCache cache)
    : IUserRegistrationService
{
    public async Task<Result<Guid>> RegisterAsync(
        UserRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = SecurityGuard.NormalizeEmail(request.Email);

        var emailExists = await userRepository.AnyAsync(
            u => u.Emails.Any(e => e.Address == normalizedEmail),
            cancellationToken);

        if (emailExists)
        {
            return Result<Guid>.Conflict(
                Error.Conflict("User.Email", "An account with this email already exists."));
        }

        var user = User.Register(normalizedEmail, request.FirstName, request.LastName);
        user.SetPasswordHash(passwordHasher.Hash(request.Password));

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(SecurityCacheKeys.UsersTag, cancellationToken);

        return Result<Guid>.Created(user.Id);
    }
}
