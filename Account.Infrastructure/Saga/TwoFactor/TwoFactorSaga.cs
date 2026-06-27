using Account.Contracts.Saga.TwoFactor.Commands;
using Account.Contracts.Saga.TwoFactor.Events;
using Account.Infrastructure.Persistence.SagaModels;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Saga.TwoFactor;

public class TwoFactorSaga : MassTransitStateMachine<TwoFactorSagaState>
{
    public static State AwaitingOtpCodeSend { get; private set; } = null!;
    public static State TwoFactorVerificationCompleted { get; private set; } = null!;
    public State TwoFactorFailed { get; private set; } = null!;

    public Event<TwoFactorSagaStartedIntegrationEvent> TwoFactorStarted { get; private set; } = null!;
    public Event<OtpCodeSentIntegrationEvent> OtpCodeSend { get; private set; } = null!;
    public Event<TwoFactorFailedIntegrationEvent> TwoFactorFailedEvent { get; private set; }

    public TwoFactorSaga(ILogger<TwoFactorSaga> logger)
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
                }).Publish(context => new SendOtpCodeIntegrationEvent
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
        }));
        //TODO add logic 
        DuringAny(When(TwoFactorFailedEvent).Then(context =>
        {
            context.Saga.FailureReason = context.Message.FailureReason ?? "Unknown failure reason";
            context.Saga.UpdatedAt = DateTime.UtcNow;
            logger.LogInformation("Two factor saga failed for UserId={UserId}. Reason: {Reason}",
                context.Saga.UserId, context.Saga.FailureReason);
        }));
    }
}