using Account.Domain.Interfaces;
using Account.Domain.Repositories;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.ChangePassword;

public class ChangePasswordHandler(
    IUserRepository userRepository,
    IPreAuthTokenService preAuthTokenService,
    ILogger<ChangePasswordHandler> logger,
    IPasswordService passwordService)
    : ICommandHandler<ChangePasswordCommand, Result<ChangePasswordResult>>
{
    public async Task<Result<ChangePasswordResult>> Handle(ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(request.Email, nameof(request.Email));
        ArgumentException.ThrowIfNullOrEmpty(request.Password, nameof(request.Password));
        ArgumentException.ThrowIfNullOrEmpty(request.PendingToken, nameof(request.PendingToken));

        var normalizedEmail = Email.Create(request.Email);
        var user = await userRepository.GetUserByEmailAsync(normalizedEmail, cancellationToken);
        if (user == null)
        {
            logger.LogWarning(
                "For change password operation, User not found with email: {MaskedEmail}",
                MaskedEmail.Create(normalizedEmail));
            return Result<ChangePasswordResult>.Conflict("");
        }

        var isValidToken = await preAuthTokenService.ValidatePendingTokenAsync(request.PendingToken, normalizedEmail);
        if (!isValidToken)
            return Result<ChangePasswordResult>.Conflict("Invalid token");

        try
        {
            var providerRes = await passwordService.ChangePasswordAsync(normalizedEmail, request.Password);
            if (!providerRes.IsSuccess)
            {
                logger.LogWarning(
                    "For change password operation, failed to change password for email: {MaskedEmail}. Error: {Error}",
                    MaskedEmail.Create(normalizedEmail), providerRes.Errors.FirstOrDefault());
                return Result<ChangePasswordResult>.Conflict(providerRes.Errors.FirstOrDefault());
            }
            //TODO implement logic
            // 1 invalidate pending token 
            // 2 change password in db 
            // 3 push to rabbit mq message  
            // 4 consumer catch from rabbit message and send email to user with info about password change 
            // 5 return token model to user with new token and api key
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred while handling ChangePasswordCommand for email {Email}",
                MaskedEmail.Create(normalizedEmail));
            throw;
        }

        throw new NotImplementedException();
    }
}