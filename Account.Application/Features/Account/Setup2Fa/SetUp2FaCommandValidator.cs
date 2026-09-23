using FluentValidation;

namespace Account.Application.Features.Account.Setup2Fa;

public class SetUp2FaCommandValidator : AbstractValidator<Setup2FaCommand>
{
    public SetUp2FaCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Incorrect format email")
            .MaximumLength(256);
    }
}