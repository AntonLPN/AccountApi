using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Repositories;
using Account.Domain.Specifications;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.ChangePassword;

public class ChangePasswordHandler(
    ILogger<ChangePasswordHandler> logger,
    IRepository<AppUser> userRepository,
    IPreAuthTokenService preAuthTokenService,
    IProviderPasswordService providerPasswordService,
    ICryptography cryptographyService,
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
        var tx = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var user = await userRepository.FirstOrDefaultAsync(
                new UserByEmailWithAuthorizedApiKeysSpec(normalizedEmail), cancellationToken);
            if (user == null)
            {
                logger.LogWarning(
                    "For change password operation, User not found with email: {MaskedEmail}",
                    MaskedEmail.Create(normalizedEmail));
                return Result<ChangePasswordResult>.Conflict("");//for safety wee don't return user not found
            }
            
            if (!await preAuthTokenService.ValidateAndConsumePendingTokenAsync(request.PendingToken, normalizedEmail))
                return Result<ChangePasswordResult>.Conflict("Invalid token");

            var providerRes = await providerPasswordService.ChangePasswordAsync(normalizedEmail, request.Password);
            if (!providerRes.IsSuccess)
            {
                logger.LogWarning(
                    "For change password operation, failed to change password for userid : {UserId}. Error: {Error}",
                    user.Id, providerRes.Errors.FirstOrDefault());
                return Result<ChangePasswordResult>.Conflict(providerRes.Errors.FirstOrDefault());
            }

            user.ChangePassword(cryptographyService.Hash(request.Password));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return Result<ChangePasswordResult>.Success(new ChangePasswordResult
            {
                IsPasswordChanged = true
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