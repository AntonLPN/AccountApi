using Account.Contracts.Events;
using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Repositories;
using Account.Domain.Specifications;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.ChangePassword;

public class ChangePasswordHandler(
    ILogger<ChangePasswordHandler> logger,
    IRepository<AppUser> userRepository,
    IPreAuthTokenService preAuthTokenService,
    IPasswordService passwordService,
    ICryptography cryptographyService,
    IPublishEndpoint publishEndpoint,
    IAuthService authService,
    IApiKeyRepository apiKeyRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ChangePasswordCommand, Result<ChangePasswordResult>>
{
    public async Task<Result<ChangePasswordResult>> Handle(ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(request.Email, nameof(request.Email));
        ArgumentException.ThrowIfNullOrEmpty(request.Password, nameof(request.Password));
        ArgumentException.ThrowIfNullOrEmpty(request.PendingToken, nameof(request.PendingToken));

        var normalizedEmail = Email.Create(request.Email);
        var user = await userRepository.FirstOrDefaultAsync(new UserByEmailSpec(normalizedEmail), cancellationToken);
        if (user == null)
        {
            logger.LogWarning(
                "For change password operation, User not found with email: {MaskedEmail}",
                MaskedEmail.Create(normalizedEmail));
            return Result<ChangePasswordResult>.Conflict("");
        }

        var isValidToken =
            await preAuthTokenService.ValidateAndConsumePendingTokenAsync(request.PendingToken, normalizedEmail);
        if (!isValidToken)
            return Result<ChangePasswordResult>.Conflict("Invalid token");

        try
        {
            var providerRes = await passwordService.ChangePasswordAsync(normalizedEmail, request.Password);
            if (!providerRes.IsSuccess)
            {
                logger.LogWarning(
                    "For change password operation, failed to change password for userid : {UserId}. Error: {Error}",
                    user.Id, providerRes.Errors.FirstOrDefault());
                return Result<ChangePasswordResult>.Conflict(providerRes.Errors.FirstOrDefault());
            }

            user.ChangePassword(cryptographyService.Hash(request.Password));
            //push to rabbit mq message  
            await publishEndpoint.Publish(new ChangePasswordIntegrationEvent
            {
                CorrelationId = Guid.NewGuid(),
                UserId = user.Id
            }, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            var tokenResponse = await authService.LoginAsync(normalizedEmail, request.Password);
            if (tokenResponse is null)
                return Result<ChangePasswordResult>.Unauthorized();

            return Result<ChangePasswordResult>.Success(new ChangePasswordResult
            {
                Token = tokenResponse,
                ApiKey = await apiKeyRepository.GetApiKeyAsync(user.Id, cancellationToken)
            });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred while handling ChangePasswordCommand for email {Email}",
                MaskedEmail.Create(normalizedEmail));
            throw;
        }
    }
}