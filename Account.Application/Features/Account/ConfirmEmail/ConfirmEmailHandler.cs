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
            if (user is null)
                return Result<bool>.NotFound("User not found");
            var otpActiveSession =
                await otpService.ValidateActiveSessionAsync(user, request.ConfirmationCode, cancellationToken);
            if (!otpActiveSession.IsSuccess)
            {
                return Result<bool>.Conflict(otpActiveSession.Errors.FirstOrDefault() ??
                                             "Invalid OTP code");
            }

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