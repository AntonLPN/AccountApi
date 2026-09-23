using FluentValidation;

namespace Account.Application.Features.Account.DeleteApiKey;

public class DeleteApiKeyCommandValidator : AbstractValidator<DeleteApiKeyCommand>
{
    public DeleteApiKeyCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Incorrect format email")
            .MaximumLength(256);
        RuleFor(x => x.ApiKey)
            .NotEmpty().WithMessage("ApiKey is required");
    }
}