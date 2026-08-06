using Account.Domain.Entities;
using Account.Domain.Enums;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Domain.Repositories;
using Account.Domain.Specifications;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.Register;

public class UserRegistrationCoordinator(ILogger<UserRegistrationCoordinator> logger,
    IRepository<AppUser> userRepository,
    IUserAccountService userAccountService,
    IUnitOfWork unitOfWork,
    ICryptography cryptographyService,
    IAuthService authService) : IUserRegistrationCoordinator
{
    public async Task<Result<RegisterUserResult>> RegisterAsync(RegisterCommand request, CancellationToken ct)
    {
        var normalizedEmail = Email.Create(request.Email);

        var keycloakResult = await userAccountService.RegisterUserAsync(normalizedEmail, request.Password);
        if (!keycloakResult.IsSuccess)
            return Result<RegisterUserResult>.Error(
                keycloakResult.Errors.FirstOrDefault() ?? "Registration failed");
        await using var tx = await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var whoInvited = await userRepository.FirstOrDefaultAsync(
                new UserByReferralCodeSpec(request.ReferrerCode), ct);

            var passwordHash = cryptographyService.Hash(request.Password);

            var user = AppUser.Create(new AppUserCreateParams(
                keycloakResult.Value,
                normalizedEmail,
                passwordHash,
                whoInvited?.Id,
                request.IpAddress,
                request.UserAgent,
                false,
                nameof(AuthProviders.LocalProvider)
            ));
            await userRepository.AddAsync(user, ct);
            await unitOfWork.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
  
            var tokenResponse = await authService.LoginAsync(normalizedEmail, request.Password);
            if (tokenResponse is not null)
                return Result<RegisterUserResult>.Success(new RegisterUserResult
                {
                    ApiKeys = user.ApiKeys.Select(k => k.ApiKeyValue).ToList(),
                    Token = tokenResponse
                });
            
            logger.LogError("Login failed after successful registration for user {UserId}", user.Id);
            return Result<RegisterUserResult>.Error(
                "Registration succeeded, but automatic login failed. Please log in manually.");

        }
        catch (Exception e)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(e, "Error occurred while registering user {Email}, rolling back external account",
                MaskedEmail.Create(normalizedEmail));

            await CompensateExternalRegistrationAsync(normalizedEmail);
            throw; 
        }

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