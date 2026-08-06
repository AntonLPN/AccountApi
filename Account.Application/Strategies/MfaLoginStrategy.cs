using Account.Application.Features.Account.Login;
using Account.Application.Interfaces;
using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Domain.ValueObjects;
using Ardalis.Result;

namespace Account.Application.Strategies;

public class MfaLoginStrategy(
    IPreAuthTokenService preAuthTokenService,
    IMfaManager mfaManager) : ILoginStrategy
{
    public bool CanHandle(AppUser user) => user.IsTwoFactorEnabled;

    public async Task<Result<LoginUserResult>> HandleAsync(
        AppUser user, LoginCommand request, CancellationToken ct)
    {
        var preAuthToken = preAuthTokenService.GeneratePreAuthToken(Email.Create(request.Email));
        await mfaManager.InitiateTwoFactorProcessAsync(user, ct);
        return Result<LoginUserResult>.Success(new LoginUserResult
        {
            IsMfaRequired = true,
            Token = new TokenResponse
            {
                AccessToken = preAuthToken,
                RefreshToken = "",
                ExpiresIn = 0,
                TokenType = "pre-auth",
                Scope = ""
            }
        });
    }
}