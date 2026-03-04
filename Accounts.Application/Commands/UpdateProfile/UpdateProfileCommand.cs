using Accounts.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.UpdateProfile;

public sealed record UpdateProfileResult(
    string FirstName,
    string LastName);

public sealed record UpdateProfileCommand(
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    Gender? Gender,
    string? Country,
    string? City,
    string? AddressLine) : ICommand<UpdateProfileResult>;
