using FluentValidation;

namespace Account.Application.Features.Account.ChekEmailAvailability;

public class CheckEmailCommandValidator : AbstractValidator<ChekEmailAvailabilityCommand>
{
    public CheckEmailCommandValidator()
    {
        
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Incorrect format email")
            .MaximumLength(256);
    }
}