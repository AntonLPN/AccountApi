using System.Diagnostics.CodeAnalysis;
using Account.Contracts.Saga.TwoFactor.Commands;
using Account.Contracts.Saga.TwoFactor.Events;
using Account.Infrastructure.Persistence.SagaModels;
using MassTransit;
using Microsoft.Extensions.Logging;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable once ClassNeverInstantiated.Global

namespace Account.Infrastructure.Saga.TwoFactor;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
public class TwoFactorSaga : MassTransitStateMachine<TwoFactorSagaState>
{
    public static State AwaitingOtpCodeSend { get; private set; } = null!;
    public static State TwoFactorVerificationCompleted { get; private set; } = null!;
    public State TwoFactorFailed { get; private set; } = null!;

    public Event<TwoFactorSagaStartedIntegrationEvent> TwoFactorStarted { get; private set; } = null!;
    public Event<OtpCodeSentIntegrationEvent> OtpCodeSend { get; private set; } = null!;
    public Event<TwoFactorFailedIntegrationEvent> TwoFactorFailedEvent { get; private set; }
    public Event<TwoFactorCompletedIntegrationEvent> TwoFactorCompleted { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public TwoFactorSaga(ILogger<TwoFactorSaga> logger)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        Initially(
            When(TwoFactorStarted)
                .Then(context =>
                {
                    context.Saga.UserId = context.Message.UserId;
                    context.Saga.Email = context.Message.Email;
                    context.Saga.OtpCode = context.Message.OtpCode;
                    context.Saga.CreatedAt = DateTime.UtcNow;
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                    context.Saga.OtpCodeSent = false;
                    context.Saga.ExpiredAt = context.Message.ExpirationTime;
                    logger.LogInformation("Two factor saga started for UserId={UserId}", context.Message.UserId);
                }).Publish(context => new SendOtpCodeIntegrationCommand
                {
                    CorrelationId = context.Saga.CorrelationId,
                    UserId = context.Message.UserId,
                    Email = context.Message.Email,
                    OtpCode = context.Message.OtpCode
                })
                .TransitionTo(AwaitingOtpCodeSend));
        During(AwaitingOtpCodeSend, When(OtpCodeSend).Then(context =>
        {
            context.Saga.OtpCodeSent = true;
            context.Saga.UpdatedAt = DateTime.UtcNow;
            logger.LogInformation("Otp code sent for UserId={UserId}", context.Saga.UserId);
        }).TransitionTo(TwoFactorVerificationCompleted));
        During(TwoFactorVerificationCompleted, When(TwoFactorCompleted).Then(context =>
        {
            context.Saga.UpdatedAt = DateTime.UtcNow;
            logger.LogInformation("Two factor saga completed for UserId={UserId}", context.Saga.UserId);
        }));
        DuringAny(When(TwoFactorFailedEvent).Then(context =>
        {
            context.Saga.FailureReason = context.Message.FailureReason ?? "Unknown failure reason";
            context.Saga.UpdatedAt = DateTime.UtcNow;
            logger.LogInformation("Two factor saga failed for UserId={UserId}. Reason: {Reason}",
                context.Saga.UserId, context.Saga.FailureReason);
        }).TransitionTo(TwoFactorFailed));
    }
}