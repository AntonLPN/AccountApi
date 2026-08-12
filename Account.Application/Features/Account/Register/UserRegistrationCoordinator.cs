using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Domain.Repositories;
using Account.Domain.Specifications;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.Register;

public class UserRegistrationCoordinator(
    ILogger<UserRegistrationCoordinator> logger,
    IRepository<AppUser> userRepository,
    IRepository<ApiKey> apiKeyRepository,
    IUserAccountService userAccountService,
    IUnitOfWork unitOfWork,
    ICryptography cryptographyService) : IUserRegistrationCoordinator
{
    public async Task<Result<RegisterUserResult>> RegisterAsync(UserCoordinatorParams request, CancellationToken ct)
    {
        var normalizedEmail = Email.Create(request.RegisterCommand.Email);
        await using var tx = await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var whoInvited = await userRepository.FirstOrDefaultAsync(
                new UserByReferralCodeSpec(request.RegisterCommand.ReferrerCode), ct);
            var passwordHash = cryptographyService.Hash(request.RegisterCommand.Password);
            var user = AppUser.Create(new AppUserCreateParams(
                request.UserId,
                normalizedEmail,
                passwordHash,
                whoInvited?.Id,
                request.RegisterCommand.IpAddress,
                request.RegisterCommand.UserAgent,
                request.RegisterCommand.EmailConfirmed,
                nameof(request.RegisterCommand.Provider)
            ));

            var key = Guid.NewGuid().ToString("N");
            var hashedKey = cryptographyService.Hash(key);
            var apiKey = ApiKey.Create(new ApiKeyCreateParams(user.Id, key, hashedKey, true));

            await apiKeyRepository.AddAsync(apiKey, ct);
            await userRepository.AddAsync(user, ct);
            await unitOfWork.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception e)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(e, "Error occurred while registering user {Email}, rolling back external account",
                MaskedEmail.Create(normalizedEmail));

            await CompensateExternalRegistrationAsync(normalizedEmail);
            throw;
        }

        return Result<RegisterUserResult>.Success(new RegisterUserResult { IsSuccess = true });
    }

    private async Task CompensateExternalRegistrationAsync(Email email)
    {
        try
        {
            await userAccountService.DeleteUserAsync(email);
        }
        catch (Exception cleanupEx)
        {
            logger.LogCritical(cleanupEx,
                "CRITICAL: Failed to compensate external account for {Email}. Manual intervention required.",
                MaskedEmail.Create(email));
        }
    }
}

// ReSharper disable once ClassNeverInstantiated.Global
public record UserCoordinatorParams(RegisterCommand RegisterCommand, string UserId);