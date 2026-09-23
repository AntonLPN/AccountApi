using FluentValidation;

namespace Account.Application.Features.Account.ProviderLogin;

public class ProviderLoginCommandValidator : AbstractValidator<ProviderLoginCommand>
{
    public ProviderLoginCommandValidator()
    {
        RuleFor(x => x.ProviderToken)
            .NotEmpty().WithMessage("Provider token is required")
            .MaximumLength(512).WithMessage("Provider token must not exceed 512 characters");
    }
}