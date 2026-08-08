using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Repositories;
using Account.Domain.Specifications;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.Logout;

public class LogoutUserHandler(
    ILogger<LogoutUserHandler> logger,
    IAuthService authService,
    IRepository<AppUser> userRepository)
    : ICommandHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = Email.Create(request.Email);
        var user = await userRepository.FirstOrDefaultAsync(new UserByEmailSpec(normalizedEmail), cancellationToken);
        if (user is null)
            return Result.Unauthorized();

        var loggedOut = await authService.LogoutAsync(request.RefreshToken);
        if (!loggedOut)
            return Result.Error("Logout failed");
        user.Logout(request.IpAddress, request.UserAgent);
        //await unitOfWork.SaveChangesAsync(cancellationToken); //need for saga

        logger.LogInformation("User {Email} logged out, logout saga started", MaskedEmail.Create(normalizedEmail));

        return Result.Success();
    }
}