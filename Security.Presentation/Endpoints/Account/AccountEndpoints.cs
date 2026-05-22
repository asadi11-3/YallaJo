using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Security.Application.Commands.ChangePassword;
using Security.Application.Commands.UpdatePhone;
using Security.Contracts.Authorization;
using Security.Presentation.Endpoints.Account.Models;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Security.Presentation.Endpoints.Account;

internal static class AccountEndpoints
{
    internal static void MapAccountEndpoints(RouteGroupBuilder group)
    {
        var account = group.MapGroup("/account");

        account.MapPut("/password", async (ChangePasswordRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new ChangePasswordCommand(request.CurrentPassword, request.NewPassword, request.ConfirmNewPassword), ct);
            return result.ToApiResult();
        })
        .WithName("ChangePassword")
        .Produces<ChangePasswordResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Change the current user's password")
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.User, AppAction.UpdateSelf))
        .RequireAuthorization();

        account.MapPut("/phone", async (UpdatePrimaryPhoneRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdatePrimaryPhoneCommand(request.PhoneNumber), ct);
            return result.ToApiResult();
        })
        .WithName("UpdatePrimaryPhone")
        .Produces<UpdatePrimaryPhoneResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Update the current user's primary phone number")
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.User, AppAction.UpdateSelf))
        .RequireAuthorization();
    }
}
