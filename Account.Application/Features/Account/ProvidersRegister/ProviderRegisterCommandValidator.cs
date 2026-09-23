using FluentValidation;

namespace Account.Application.Features.Account.ProvidersRegister;

public class ProviderRegisterCommandValidator : AbstractValidator<ProviderRegisterCommand>
{
    public ProviderRegisterCommandValidator()
    {
        RuleFor(x => x.ProviderToken)
            .NotEmpty().WithMessage("Provider token is required")
            .MaximumLength(512);
    }
}