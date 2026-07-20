using Account.Contracts.SagaEvents.UserLogoutSagaEvents.Events;
using Account.Domain.Interfaces;
using Account.Domain.Repositories;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.Logout;

public class LogoutUserHandler(
    ILogger<LogoutUserHandler> logger,
    IAuthService authService,
    IUnitOfWork unitOfWork,
    IUserRepository userRepository,
    IPublishEndpoint publishEndpoint)
    : ICommandHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = Email.Create(request.Email);
        var user = await userRepository.GetUserByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null)
            return Result.Unauthorized();

        var loggedOut = await authService.LogoutAsync(request.RefreshToken);
        if (!loggedOut)
            return Result.Error("Logout failed");

        await publishEndpoint.Publish(new UserLogoutSagaStartedIntegrationEvent
        {
            CorrelationId = Guid.NewGuid(),
            UserId = user.Id,
            Email = user.Email,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken); //need for saga

        logger.LogInformation("User {Email} logged out, logout saga started", MaskedEmail.Create(normalizedEmail));

        return Result.Success();
    }
}