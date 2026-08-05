using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Repositories;
using Microsoft.Extensions.Logging;

// ReSharper disable InconsistentNaming

namespace Account.Infrastructure.Services;

// ReSharper disable once ClassNeverInstantiated.Global
public class MfaService(
    IUnitOfWork unitOfWork,
    ILogger<MfaService> logger,
    IOtpService otpService) : IMfaManager
{
    
    public async Task<string> InitiateTwoFactorProcessAsync(
        AppUser user,
        CancellationToken cancellationToken)
    {
        var otpCode = otpService.GenerateOtpCode(user);
        var correlationId = Guid.NewGuid();

        await using var tx = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await otpService.InvalidateOtpSessionsAsync(user.Id, cancellationToken);
            await otpService.CreateOtpSessionAsync(user.Id, otpCode, correlationId, cancellationToken);
            user.InitiateTwoFactorAuthentication(otpCode);
            
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return otpCode;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred while initiating two-factor process for user {UserId}", user.Id);
            throw;
        }
    }
}