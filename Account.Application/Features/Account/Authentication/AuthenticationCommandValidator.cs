using FluentValidation;

namespace Account.Application.Features.Account.Authentication;

public class AuthenticationCommandValidator : AbstractValidator<AuthenticationCommand>
{
    public AuthenticationCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required")
            .MaximumLength(512);
    }
}