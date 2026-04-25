// Security.Domain/Errors/UserErrors.cs
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Domain.Errors;

public static class UserErrors
{
    public static readonly Error NotFound = new("NotFound.User", "The specified user was not found.");

    public static readonly Error Unauthorized = new(
        "Unauthorized.User",
        "Authentication is required.");

    public static readonly Error InsufficientPrivilege = new(
        "Forbidden.InsufficientPrivilege",
        "You do not have sufficient privilege to manage this user.");

    public static readonly Error SameLevelForbidden = new(
        "Forbidden.SameLevel",
        "You cannot manage a user at the same privilege level as yourself.");

    public static readonly Error CannotManageSelfPrivilege = new(
        "Forbidden.SelfPrivilege",
        "You cannot modify your own privileged roles or claims.");

    public static readonly Error RoleBelowActor = new(
        "Forbidden.RoleBelowActor",
        "You cannot assign or remove a role at or above your own privilege level.");
}

