using Account.Contracts.Saga.TwoFactor.Events;
using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Domain.Repositories;
using Account.Domain.Specifications;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using MassTransit;
using Microsoft.Extensions.Logging;
using OtpNet;

namespace Account.Application.Features.Account.OtpCodeVerification;

public class OtpCodeVerificationHandler(
    ILogger<OtpCodeVerificationHandler> logger,
    IRepository<AppUser> userRepository,
    IRepository<OtpSessions> otpSessionRepository,
    IUnitOfWork unitOfWork,
    IAuthService authService,
    IPublishEndpoint publishEndpoint,
    IOtpService otpService)
    : ICommandHandler<OtpCodeVerificationCommand, Result<OtpConfirmationResult>>
{
    public async Task<Result<OtpConfirmationResult>> Handle(OtpCodeVerificationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(request.OtpCode, nameof(request.OtpCode));

        var normalizedEmail = Email.Create(request.Email);
        try
        {
            var user = await userRepository.FirstOrDefaultAsync(
                new UserByEmailWithAuthorizedApiKeysSpec(normalizedEmail),
                cancellationToken);
            if (user is null)
                return Result<OtpConfirmationResult>.NotFound("User not found");
            var otpActiveSession =
                await otpService.ValidateActiveSessionAsync(user, request.OtpCode, cancellationToken);
            if (!otpActiveSession.IsSuccess)
            {
                return Result<OtpConfirmationResult>.Conflict(otpActiveSession.Errors.FirstOrDefault() ??
                                                              "Invalid OTP code");
            }

            TokenResponse? tokenResponse = await authService.LoginAsync(normalizedEmail);
            if (tokenResponse is null)
                return Result<OtpConfirmationResult>.Unauthorized("Login failed after OTP verification");

            otpActiveSession.Value.UsedAt = DateTime.UtcNow;
            await otpSessionRepository.UpdateAsync(otpActiveSession, cancellationToken);

            await publishEndpoint.Publish(new OtpCodeConfirmedIntegrationEvent()
            {
                CorrelationId = Guid.NewGuid(),
                UserId = user.Id,
                IsValid = true,
            }, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("OTP verification successful for user {UserId}", user.Id);
            return Result<OtpConfirmationResult>.Success(new OtpConfirmationResult()
            {
                ApiKeys = user.ApiKeys.Select(k => k.ApiKeyValue).ToList(),
                Token = tokenResponse,
            });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during OTP verification for email {Email}", MaskedEmail.Create(normalizedEmail));
            throw; //rethrow to middleware handle exception
        }
    }
}