using FluentValidation;

namespace Account.Application.Features.Account.OtpCodeVerification;

public class OtpCodeVerificationCommandValidator : AbstractValidator<OtpCodeVerificationCommand>
{
    public OtpCodeVerificationCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Incorrect format email")
            .MaximumLength(256);
        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("OtpCode is required");
    }
}