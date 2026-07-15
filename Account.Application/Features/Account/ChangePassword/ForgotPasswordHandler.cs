using Account.Domain.Interfaces;
using Account.Domain.Repositories;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.ChangePassword;

public class ForgotPasswordHandler(
    ILogger<ForgotPasswordHandler> logger,
    IUserRepository userRepository,
    IAuthService authService,
    ITwoFactorManager twoFactorManager)
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
            var token = authService.GeneratePreAuthToken(request.Email);
           // var pendingToken = 
            return Result<ForgotPasswordResult>.Success(new ForgotPasswordResult()
            {
                AccessToken = token,
                PendingToken = "sxs-otp-pending"//TODO: generate a pending token for the user to use in the next step of the password reset process
            });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred while handling ForgotPasswordCommand");
            throw;
        }
    }
}