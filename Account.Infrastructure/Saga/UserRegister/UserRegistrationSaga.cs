using System.Diagnostics.CodeAnalysis;
using Account.Contracts.Events.External;
using Account.Contracts.Saga.UserRegisterSagaEvents.Commands;
using Account.Contracts.Saga.UserRegisterSagaEvents.Events;
using Account.Infrastructure.Persistence.SagaModels;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Saga.UserRegister;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
// ReSharper disable once ClassNeverInstantiated.Global
public class UserRegistrationSaga : MassTransitStateMachine<UserRegistrationSagaState>
{
    public State AwaitingWelcomeEmailSent { get; private set; } = null!;
    public State AwaitingProfileInitialization { get; private set; } = null!;
    public State RegistrationCompleted { get; private set; } = null!;
    public State RegistrationFailed { get; private set; } = null!;

    public Event<UserSagaStartedIntegrationEvent> RegistrationStarted { get; private set; } = null!;
    public Event<WelcomeEmailSentIntegrationEvent> WelcomeEmailSent { get; private set; } = null!;
    public Event<UserProfileInitializedIntegrationEvent> ProfileInitialized { get; private set; } = null!;
    public Event<UserRegistrationSagaFailedIntegrationEvent> RegistrationFailedEvent { get; private set; } = null!;

    public UserRegistrationSaga(ILogger<UserRegistrationSaga> logger)
    {
        Event(() => RegistrationStarted, x => x.CorrelateById(context => context.Message.CorrelationId));
        Event(() => WelcomeEmailSent, x => x.CorrelateById(context => context.Message.CorrelationId));
        Event(() => ProfileInitialized, x => x.CorrelateById(context => context.Message.CorrelationId));
        Event(() => RegistrationFailedEvent, x => x.CorrelateById(context => context.Message.CorrelationId));
        
        InstanceState(x => x.CurrentState);

        Initially(
            When(RegistrationStarted)
                .Then(context =>
                {
                    context.Saga.UserId = context.Message.UserId;
                    context.Saga.Email = context.Message.Email;
                    context.Saga.ApiKey = context.Message.ApiKey;
                    context.Saga.CreatedAt = DateTime.UtcNow;
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                    logger.LogInformation("Saga registration started for UserId={UserId}", context.Message.UserId);
                })
                .Publish(context => new SendWelcomeEmailIntegrationEvent
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        UserId = context.Message.UserId,
                        Email = context.Message.Email,
                        ApiKey = context.Message.ApiKey
                    }
                )
                .Publish(context =>
                    new UserRegisteredIntegrationEvent() //use this event to send values to another microservice
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        UserId = context.Saga.UserId,
                        Email = context.Saga.Email,
                        ApiKey = context.Saga.ApiKey,
                        ReferralCode = context.Message.ReferralCode,
                        IsActive = context.Message.IsActive,
                        EmailConfirmed = context.Message.EmailConfirmed
                    })
                .TransitionTo(AwaitingWelcomeEmailSent));
        During(AwaitingWelcomeEmailSent,
            When(WelcomeEmailSent).Then(context =>
                {
                    context.Saga.EmailConfirmationSent = true;
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                    logger.LogInformation("Email confirmation sent for UserId={UserId}", context.Saga.UserId);
                }).Publish(context => new InitializeUserProfileIntegrationEvent
                {
                    CorrelationId = context.Saga.CorrelationId,
                    UserId = context.Saga.UserId,
                    Email = context.Saga.Email,
                    ApiKey = context.Saga.ApiKey
                })
                .TransitionTo(AwaitingProfileInitialization));
        During(AwaitingProfileInitialization,
            When(ProfileInitialized).Then(context =>
                {
                    context.Saga.ProfileInitialized = true;
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                    logger.LogInformation("Profile initialized for UserId={UserId}", context.Saga.UserId);
                })
                .TransitionTo(RegistrationCompleted));
        DuringAny(
            When(RegistrationFailedEvent)
                .Then(context =>
                {
                    context.Saga.FailureReason = context.Message.FailureReason ?? "Unknown failure reason";
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                    logger.LogError("User registration failed for UserId={UserId}. Reason: {Reason}",
                        context.Saga.UserId, context.Message.FailureReason);
                })
                .Publish(context => new UserRegistrationSagaFailedIntegrationEvent
                {
                    UserId = context.Saga.UserId,
                    FailureReason = context.Message.FailureReason
                })
                .TransitionTo(RegistrationFailed));
    }
}