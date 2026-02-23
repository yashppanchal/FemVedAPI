using FluentValidation;

namespace FemVed.Application.Admin.Commands.ChangeUserRole;

/// <summary>Validates <see cref="ChangeUserRoleCommand"/>.</summary>
public sealed class ChangeUserRoleCommandValidator : AbstractValidator<ChangeUserRoleCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public ChangeUserRoleCommandValidator()
    {
        RuleFor(x => x.TargetUserId)
            .NotEmpty().WithMessage("userId is required.");

        RuleFor(x => x.RoleId)
            .InclusiveBetween((short)1, (short)3)
            .WithMessage("RoleId must be 1 (Admin), 2 (Expert), or 3 (User).");
    }
}
