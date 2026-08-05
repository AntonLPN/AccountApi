using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Specifications;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.ConfirmEmail;

public class ConfirmEmailHandler(
    ILogger<ConfirmEmailHandler> logger,
    IRepository<AppUser> userRepository,
    ICryptography cryptographyService,
    IRepository<OtpSessions> otpSessionRepository,
    IOtpService otpService)
    : ICommandHandler<ConfirmEmailCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = Email.Create(request.Email);
        try
        {
            var user = await userRepository.FirstOrDefaultAsync(
                new UserByEmailSpec(normalizedEmail),
                cancellationToken);
            //TODO this part should be moved to a separate service
            var otpCodeHash = cryptographyService.Hash(request.ConfirmationCode);
            var otpActiveSession =
                await otpSessionRepository.FirstOrDefaultAsync(new OtpGetActiveSessionSpec(user.Id, otpCodeHash),
                    cancellationToken);
            if (otpActiveSession == null || otpActiveSession.UsedAt != null)
                return Result<bool>.NotFound(
                    "No active OTP session found for the user or OTP already used");

            if (otpActiveSession.ExpiresAt < DateTime.UtcNow)
            {
                logger.LogWarning("OTP session expired for user {UserId}", user.Id);
                return Result<bool>.Conflict("OTP session expired");
            }

            var isVerified = otpService.VerifyOtpCode(user, request.ConfirmationCode);
            if (!isVerified)
            {
                logger.LogWarning("Invalid OTP attempt for user {UserId}", user.Id);
                return Result<bool>.Conflict("Invalid OTP code");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred while handling ConfirmEmailCommand");
            throw;
        }

        throw new NotImplementedException();
    }
}