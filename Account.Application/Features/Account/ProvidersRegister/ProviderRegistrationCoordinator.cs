using Account.Domain.DTOs;
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

namespace Account.Application.Features.Account.ProvidersRegister;

public class ProviderRegistrationCoordinator(
    ILogger<ProviderRegistrationCoordinator> logger,
    IUserAccountService userAccountService,
    IUnitOfWork unitOfWork,
    IRepository<AppUser> userRepository,
    IRepository<ApiKey> apiKeyRepository,
    IRepository<LoginAudit> loginAuditRepository,
    IAuthService authService)
    : IProviderRegistrationCoordinator
{
    public async Task<Result<ProviderRegisterResult>> RegisterAsync(ProviderRegisterCommand request, string email, CancellationToken ct)
    {
        var registerResult = await userAccountService.RegisterUserAsync(email, "", false);
        if (!registerResult.IsSuccess)
            return Result<ProviderRegisterResult>.Error(registerResult.Errors.FirstOrDefault() ?? "Registration failed");

        var userId = registerResult.Value;

        try
        {
            await using var tx = await unitOfWork.BeginTransactionAsync(ct);

            var whoInvited = await userRepository.FirstOrDefaultAsync(
                new UserByReferralCodeSpec(request.ReferrerCode), ct);

            var user = AppUser.Create(new AppUserCreateParams(
                userId, email, null, whoInvited?.Id,
                request.IpAddress, request.UserAgent, true, nameof(AuthProviders.Google)));
            await userRepository.AddAsync(user, ct);

            var apiKey = ApiKey.Create(new ApiKeyCreateParams(user.Id, true));
            await apiKeyRepository.AddAsync(apiKey, ct);

            var loginAudit = LoginAudit.Create(new CreateLoginAuditParams
            {
                UserId = user.Id, Email = email, IpAddress = request.IpAddress,
                UserAgent = request.UserAgent, IsSuspicious = false, LoggedInAt = DateTime.UtcNow
            });
            await loginAuditRepository.AddAsync(loginAudit, ct);

            await unitOfWork.SaveChangesAsync(ct);

            var token = await authService.LoginAsync(email);
            if (token is null)
            {
                await tx.RollbackAsync(ct);
                await CompensateExternalRegistrationAsync(email);
                return Result<ProviderRegisterResult>.Error("Registration succeeded, but login failed. Please try logging in.");
            }
            
            await tx.CommitAsync(ct);

            return Result<ProviderRegisterResult>.Success(new ProviderRegisterResult
            {
                ApiKeys = user.ApiKeys.Select(k => k.ApiKeyValue).ToList(),
                Token = token
            });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred while handling Provider registration");
            await CompensateExternalRegistrationAsync(email);
            throw;
        }
    }

    private async Task CompensateExternalRegistrationAsync(string email)
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