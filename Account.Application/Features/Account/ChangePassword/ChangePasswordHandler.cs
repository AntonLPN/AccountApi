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

        var user = await userRepository.GetUserByEmailAsync(request.Email, cancellationToken);
        if (user == null)
        {
            logger.LogWarning(
                $"For change password operation, User not found with email: {MaskedEmail.Create(request.Email)}");
            return Result<ChangePasswordResult>.Conflict("");
        }

        var isValidToken = await preAuthTokenService.ValidatePendingTokenAsync(request.PendingToken, request.Email);
        if (!isValidToken)
            return Result<ChangePasswordResult>.Conflict("Invalid token");

        try
        {
            var providerRes = await passwordService.ChangePasswordAsync(request.Email, request.Password);
            if (!providerRes.IsSuccess)
                return Result<ChangePasswordResult>.Conflict(providerRes.Errors.FirstOrDefault());
            
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        throw new NotImplementedException();
    }
}