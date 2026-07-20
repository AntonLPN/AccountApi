using Account.Domain.Interfaces;
using Account.Domain.Repositories;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.ChangePassword;

public class ForgotPasswordHandler(
    ILogger<ForgotPasswordHandler> logger,
    IUserRepository userRepository,
    IMfaManager mfaManager,
    IPreAuthTokenService preAuthTokenService)
    : ICommandHandler<ForgotPasswordCommand, Result<ForgotPasswordResult>>
{
    public async Task<Result<ForgotPasswordResult>> Handle(ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = Email.Create(request.Email);
        try
        {
            var user = await userRepository.GetUserByEmailAsync(normalizedEmail, cancellationToken);
            if (user is null)
                return Result<ForgotPasswordResult>.NotFound("User not found");

            await mfaManager.InitiateTwoFactorProcessAsync(user, cancellationToken);
            var token = preAuthTokenService.GeneratePreAuthToken(normalizedEmail);
            var pendingToken = await preAuthTokenService.GeneratePendingTokenAsync(normalizedEmail);
            return Result<ForgotPasswordResult>.Success(new ForgotPasswordResult()
            {
                AccessToken = token,
                PendingToken = pendingToken
            });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred while handling ForgotPasswordCommand");
            throw;
        }
    }
}