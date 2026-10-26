using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Domain.Specifications;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.SendOtpCode;

public class GenerateOtpCodeHandler(
    ILogger<GenerateOtpCodeHandler> logger,
    IRepository<AppUser> userRepository,
    IPreAuthTokenService preAuthTokenService,
    IMfaManager mfaManager
) : ICommandHandler<GenerateOtpCodeCommand, Result<GenerateOtpCodeResult>>
{
    public async Task<Result<GenerateOtpCodeResult>> Handle(GenerateOtpCodeCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.FirstOrDefaultAsync(new UserByEmailSpec(request.Email), cancellationToken);
        if (user == null)
        {
            logger.LogWarning("User not found for email: {Email}", MaskedEmail.Create(request.Email));
            return Result<GenerateOtpCodeResult>.NotFound();
        }

        var preAuthToken = preAuthTokenService.GeneratePreAuthToken(Email.Create(request.Email));
        await mfaManager.InitiateTwoFactorProcessAsync(user, cancellationToken);
        return Result<GenerateOtpCodeResult>.Success(new GenerateOtpCodeResult
        {
            Token = new TokenResponse()
            {
                AccessToken = preAuthToken,
                RefreshToken = "",
                ExpiresIn = 900,
                TokenType = "pre-auth",
                Scope = ""
            }
        });
    }
}