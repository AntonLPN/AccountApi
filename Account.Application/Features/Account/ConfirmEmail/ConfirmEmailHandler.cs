using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Specifications;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.ConfirmEmail;

public class ConfirmEmailHandler(
    ILogger<ConfirmEmailHandler> logger,
    IRepository<AppUser> userRepository,
    IDataCache dataCache)
    : ICommandHandler<ConfirmEmailCommand, Result<bool>>
{
    private const string PREFIX = "email_verification_";
    public async Task<Result<bool>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var email = await dataCache.ConsumeAsync($"{PREFIX}{request.Token}");
            ArgumentNullException.ThrowIfNull(email);
            var user = await userRepository.FirstOrDefaultAsync(
                new UserByEmailSpec(email),
                cancellationToken);
            if (user is null)
                return Result<bool>.NotFound("User not found");
            if(user.EmailConfirmed)
                return Result<bool>.Success(true);
            
            user.ConfirmEmail();
            await userRepository.UpdateAsync(user, cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred while handling ConfirmEmailCommand");
            throw;
        }
    }
}