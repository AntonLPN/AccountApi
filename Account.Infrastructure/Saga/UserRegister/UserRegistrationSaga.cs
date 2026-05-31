using Account.Contracts.SagaEvents.UserRegisterSagaEvents;
using Account.Infrastructure.Persistence.SagaModels;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Saga.UserRegister;

public class UserRegistrationSaga : MassTransitStateMachine<UserRegistrationSagaState>
{
    private ILogger<UserRegistrationSaga> _logger;
    public State AwaitingEmailConfirmation { get; private set; } = null!;
    public State AwaitingProfileInitialization { get; private set; } = null!;
    public State RegistrationCompleted { get; private set; } = null!;
    public State RegistrationFailed { get; private set; } = null!;

    public Event<UserSagaStartedIntegrationEvent> RegistrationStarted { get; private set; } = null!;
    public Event<EmailConfirmationSentIntegrationEvent> EmailConfirmationSent { get; private set; } = null!;

    public UserRegistrationSaga(ILogger<UserRegistrationSaga> logger)
    {
        _logger = logger;

        Event(() => RegistrationStarted, x => x.CorrelateById(context => context.Message.CorrelationId));

        InstanceState(x => x.CurrentState);

        Initially(
            When(RegistrationStarted)
                .Then(context =>
                {
                    context.Saga.UserId = context.Message.UserId;
                    context.Saga.Email = context.Message.Email;
                    _logger.LogInformation("Saga registration started for UserId={UserId}", context.Message.UserId);
                })
                .Publish(context => new SendEmailConfirmationCommandIntegrationEvent
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        UserId = context.Message.UserId,
                        Email = context.Message.Email
                    }
                )
                .TransitionTo(AwaitingEmailConfirmation));
        During(AwaitingEmailConfirmation,
            When(EmailConfirmationSent).Then(context =>
            {
                context.Saga.EmailConfirmationSent = true;

                _logger.LogInformation("Email confirmation sent for UserId={UserId}", context.Saga.UserId);
            }).TransitionTo(AwaitingProfileInitialization));
//TODO add profile initialization
    }
}