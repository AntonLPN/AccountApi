using Account.Domain.Entities;
using Account.Domain.Specifications;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.AccountInfo;

public class AccountInfoHandler(ILogger<AccountInfoHandler> logger, IRepository<AppUser> userRepository)
    : ICommandHandler<AccountInfoCommand, Result<AccountInfoResult>>
{
    public async Task<Result<AccountInfoResult>> Handle(AccountInfoCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                logger.LogWarning("Email is null or empty in AccountInfoCommand");
                return Result<AccountInfoResult>.Invalid(new ValidationError("Email", "Email cannot be null or empty"));
            }

            var user = await userRepository.FirstOrDefaultAsync(new UserByEmailSpec(request.Email), cancellationToken);
            if (user is null)
                return Result<AccountInfoResult>.NotFound("User not found");
            var result = new AccountInfoResult
            {
                Email = user.Email,
                ReferralCode = user.ReferralCode,
                IsTwoFactorEnabled = user.IsTwoFactorEnabled,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            return Result<AccountInfoResult>.Success(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error handling AccountInfoCommand for email {Email}", MaskedEmail.Create(request.Email));
            throw;
        }
    }
}