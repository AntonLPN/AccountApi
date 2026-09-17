using Account.Domain.Entities;
using Account.Domain.Specifications;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.IsEmailVerified;

public class IsEmailVerifiedHandler(ILogger<IsEmailVerifiedHandler> logger, IRepository<AppUser> userRepository)
    : ICommandHandler<IsEmailVerifiedCommand, Result<IsEmailVerifiedResult>>
{
    public async Task<Result<IsEmailVerifiedResult>> Handle(IsEmailVerifiedCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            logger.LogWarning("Email is null or empty in IsEmailVerifiedCommand");
            return Result<IsEmailVerifiedResult>.Invalid(
                new ValidationError("Email", "Email cannot be null or empty"));
        }

        Email normalizedEmail;
        try
        {
            normalizedEmail = Email.Create(request.Email);
        }
        catch (Exception e) when (e is ArgumentException or FormatException)
        {
            logger.LogWarning(e, "Invalid email format in IsEmailVerifiedCommand");
            return Result<IsEmailVerifiedResult>.Invalid(new ValidationError("Email", "Invalid email address"));
        }

        try
        {
            var user = await userRepository.FirstOrDefaultAsync(new UserByEmailSpec(normalizedEmail),
                cancellationToken);
            if (user is null)
                return Result<IsEmailVerifiedResult>.NotFound("User not found");

            return Result<IsEmailVerifiedResult>.Success(new IsEmailVerifiedResult
            {
                Email = MaskedEmail.Create(user.Email),
                IsEmailVerified = user.EmailConfirmed
            });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred while handling IsEmailVerifiedCommand for email {Email}",
                MaskedEmail.Create(normalizedEmail));
            throw;
        }
    }
}
