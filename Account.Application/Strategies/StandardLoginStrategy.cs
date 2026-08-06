using Account.Application.Features.Account.Login;
using Account.Application.Interfaces;
using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Repositories;
using Ardalis.Result;

namespace Account.Application.Strategies;

public class StandardLoginStrategy(
    IAuthService authService,
    IUnitOfWork unitOfWork) : ILoginStrategy
{
    public bool CanHandle(AppUser user) => !user.IsTwoFactorEnabled;

    public async Task<Result<LoginUserResult>> HandleAsync(
        AppUser user, LoginCommand request, CancellationToken ct)
    {
     
        var tokenResponse = await authService.LoginAsync(user.Email, request.Password);
        if (tokenResponse is null)
            return Result<LoginUserResult>.Error("Invalid credentials or user is locked");

        user.RecordLogin(request.IpAddress, request.UserAgent);
        await unitOfWork.SaveChangesAsync(ct);
        
        return Result<LoginUserResult>.Success(new LoginUserResult
        {
            IsMfaRequired = false,
            ApiKeys = user.ApiKeys.Select(k => k.ApiKeyValue).ToList(),
            Token = tokenResponse
        });
    }
}