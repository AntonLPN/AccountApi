using Account.Domain.Repositories;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;
using OtpNet;

namespace Account.Application.Features.Account.OtpCodeVerification;

public class OtpCodeVerificationHandler(ILogger<OtpCodeVerificationHandler> logger, IUserRepository userRepository) : ICommandHandler<OtpCodeVerificationCommand, Result<OtpConfirmationResult>>
{
    public async  Task<Result<OtpConfirmationResult>> Handle(OtpCodeVerificationCommand request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(request.OtpCode, nameof(request.OtpCode));
        
        var user = await userRepository.GetUserByEmailAsync(request.Email, cancellationToken);
        if (user is null)
            return Result<OtpConfirmationResult>.Unauthorized("User not found");

        var secretKey = Convert.FromBase64String(user.EncryptedTwoFactorSecret);
        var totp = new Totp(secretKey, step: 300, mode: OtpHashMode.Sha1, totpSize: 6);
        bool isValid = totp.VerifyTotp(request.OtpCode, out long timeStepMatched, VerificationWindow.RfcSpecifiedNetworkDelay);
            
        if (!isValid)
        {
            logger.LogWarning("Invalid OTP attempt for user {UserId}", user.Id);
            return Result<OtpConfirmationResult>.Unauthorized("Invalid OTP code");
        }
        
        
        throw new NotImplementedException();
    }
}