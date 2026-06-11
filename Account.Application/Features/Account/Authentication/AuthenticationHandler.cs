using Account.Domain.Interfaces;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.Authentication;

public class AuthenticationHandler(ILogger<AuthenticationHandler> logger, IAuthService authService)
    : ICommandHandler<AuthenticationCommand, Result<AuthenticationResult>>
{
     public async Task<Result<AuthenticationResult>> Handle(AuthenticationCommand request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(request.RefreshToken, nameof(request.RefreshToken));
        var res = await authService.RefreshTokenAsync(request.RefreshToken); 
        if(res == null)
            return Result<AuthenticationResult>.Unauthorized();
        logger.LogInformation("Refresh token refreshed: {RefreshToken}", res.RefreshToken);
        return Result<AuthenticationResult>.Success(new AuthenticationResult
        {
            Token = res
        });
    }
}