using FluentValidation;

namespace Auth.Application.Commands.ProvisionAccount;

/// <summary>
/// Input validation for <see cref="ProvisionAccountCommand"/>. Mirrors the
/// field rules historically enforced by <c>InviteUserCommandValidator</c>
/// so the legacy façade path and any direct callers behave identically at
/// the validation boundary.
/// </summary>
public sealed class ProvisionAccountCommandValidator : AbstractValidator<ProvisionAccountCommand>
{
    public ProvisionAccountCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.DisplayName)
            .MaximumLength(200);

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(2048);

        RuleFor(x => x.InitialRoleIds)
            .NotNull()
            .Must(ids => ids.Count > 0)
            .WithMessage("At least one initial role must be selected.");

        RuleForEach(x => x.InitialRoleIds)
            .NotEmpty()
            .WithMessage("Role ID is required.");

        RuleFor(x => x.InitialRoleIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate roles are not allowed.");
    }
}
