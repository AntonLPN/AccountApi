using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Specifications;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.ResendOtpCode;

public class ResendOtpCodeHandler(
    ILogger<ResendOtpCodeHandler> logger,
    IRepository<AppUser> userRepository,
    IMfaManager mfaManager)
    : ICommandHandler<ResendOtpCodeCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ResendOtpCodeCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = Email.Create(request.Email);
        try
        {
            var user = await userRepository.FirstOrDefaultAsync(
                new UserByEmailSpec(normalizedEmail),
                cancellationToken);
            if (user is null)
                return Result<bool>.NotFound("User not found");

            await mfaManager.InitiateTwoFactorProcessAsync(user, cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred while resending OTP code");
            throw;
        }
    }
}
