using FluentValidation;

namespace Account.Application.Features.Account.ChangePassword;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Incorrect format email")
            .MaximumLength(256);
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long")
            .Matches(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{6,}$").WithMessage(
                "Password must contain at least one uppercase letter, one number, and one special character.")
            .MaximumLength(256);
        RuleFor(x => x.PendingToken)
            .NotEmpty().WithMessage("Pending token is required")
            .MaximumLength(256);
        
    }
}