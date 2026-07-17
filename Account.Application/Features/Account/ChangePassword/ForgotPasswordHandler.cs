using Account.Domain.Interfaces;
using Account.Domain.Repositories;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.ChangePassword;

public class ForgotPasswordHandler(
    ILogger<ForgotPasswordHandler> logger,
    IUserRepository userRepository,
    ITwoFactorManager twoFactorManager,
    IPreAuthTokenService preAuthTokenService)
    : ICommandHandler<ForgotPasswordCommand, Result<ForgotPasswordResult>>
{
    public async Task<Result<ForgotPasswordResult>> Handle(ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(request.Email, nameof(request.Email));
        try
        {
            var user = await userRepository.GetUserByEmailAsync(request.Email, cancellationToken);
            if (user is null)
                return Result<ForgotPasswordResult>.NotFound("User not found");

            await twoFactorManager.InitiateTwoFactorProcessAsync(user, cancellationToken);
            var token = preAuthTokenService.GeneratePreAuthToken(request.Email);
            var pendingToken = await preAuthTokenService.GeneratePendingTokenAsync(request.Email);
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