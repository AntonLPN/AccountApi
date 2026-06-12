using System.IdentityModel.Tokens.Jwt;
using Account.Application.Features.Account.Register;
using Account.Contracts.SagaEvents.UserRegisterSagaEvents.Events;
using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Domain.Repositories;
using Ardalis.Result;
using Ardalis.SharedKernel;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.GoogleRegister;

public class GoogleRegisterHandler(
    ILogger<GoogleRegisterHandler> logger,
    IUserRepository userRepository,
    IAuthService authService,
    IUnitOfWork unitOfWork,
    IApiKeyRepository apiKeyRepository,
    IPublishEndpoint publishEndpoint)
    : ICommandHandler<GoogleRegisterCommand, Result<GoogleRegisterResult>>
{
    public async Task<Result<GoogleRegisterResult>> Handle(GoogleRegisterCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var googlePayload = await authService.GoogleValidateAsync(request.GoogleToken);
            var email = googlePayload.Email;
            ArgumentException.ThrowIfNullOrEmpty(email);
            
            var userByEmail = await userRepository.GetUserByEmailAsync(email, cancellationToken);
            if (userByEmail is not null)
                return Result<GoogleRegisterResult>.Conflict("User already exists");
            
            var keycloakResult = await authService.RegisterUserAsync(request.GoogleToken,"",false);
            if (!keycloakResult.IsSuccess)
                return Result<GoogleRegisterResult>.Error("Google registration failed");

            var whoInvited = await userRepository.FindByReferralCodeAsync(request.ReferrerCode, cancellationToken);
            //Save to DB
            await using var tx = await unitOfWork.BeginTransactionAsync(cancellationToken);
            var user = AppUser.Create(
                id: keycloakResult.Value,
                email: email,
                passwordHash: "",
                referrerId: whoInvited?.Id,
                emailConfirmed: true,
                providerName: "Google");

            userRepository.AddUser(user);
            var apiKey = apiKeyRepository.CreateApiKey(user.Id);
            //Start Saga
            await publishEndpoint.Publish(new UserSagaStartedIntegrationEvent
            {
                CorrelationId = Guid.NewGuid(),
                UserId = user.Id,
                Email = user.Email,
                ApiKey = apiKey
            }, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            TokenResponse? tokenResponse = await authService.LoginAsync(email, "");
            if (tokenResponse is null)
                return Result<GoogleRegisterResult>.Error("Login failed after registration for user");
            return Result<GoogleRegisterResult>.Success(new GoogleRegisterResult
            {
                ApiKey = apiKey,
                Token = tokenResponse,
            });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred while handling GoogleRegisterCommand");
            throw;
        }

    }

    private string? GetEmailWithoutValidation(string googleToken)
    {
        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(googleToken))
        {
            return null;
        }

        var jwtToken = handler.ReadJwtToken(googleToken);
        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "email");

        return emailClaim?.Value;
    }
}