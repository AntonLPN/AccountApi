using FluentValidation;

namespace Account.Application.Features.Account.IsEmailVerified;

public class IsEmailVerifiedCommandValidator : AbstractValidator<IsEmailVerifiedCommand>
{
    public IsEmailVerifiedCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Incorrect format email")
            .MaximumLength(256);
    }
}