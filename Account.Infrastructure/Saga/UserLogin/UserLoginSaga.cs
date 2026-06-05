using System.Diagnostics.CodeAnalysis;
using Account.Contracts.SagaEvents.UserLoginSagaEvents.Commands;
using Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;
using Account.Infrastructure.Persistence.SagaModels;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Saga.UserLogin;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class UserLoginSaga : MassTransitStateMachine<UserLoginSagaState>
{
    public State AwaitingSuspiciousCheck { get; private set; } = null!;
    public State AwaitingAuditRecorded { get; private set; } = null!;
    public State AwaitingLastLoginUpdate { get; private set; } = null!;
    public State AwaitingLoginNotification { get; private set; } = null!;
    public State LoginCompleted { get; private set; } = null!;
    public State LoginFailed { get; private set; } = null!;


    public Event<UserLoginSagaStartedIntegrationEvent> LoginStarted { get; private set; } = null!;
    public Event<SuspiciousLoginCheckedIntegrationEvent> SuspiciousLoginChecked { get; private set; } = null!;
    public Event<LoginAuditRecordedIntegrationEvent> LoginAuditRecorded { get; private set; } = null!;
    public Event<LastLoginUpdatedIntegrationEvent> LastLoginUpdated { get; private set; } = null!;
    public Event<LoginNotificationSentIntegrationEvent> LoginNotificationSent { get; private set; } = null!;
    public Event<UserLoginSagaFailedIntegrationEvent> LoginFailedEvent { get; private set; } = null!;

    public UserLoginSaga(ILogger<UserLoginSaga> logger)
    {
        InstanceState(x => x.CurrentState);

        Event(() => LoginStarted, x => x.CorrelateById(context => context.Message.CorrelationId));
        Event(() => SuspiciousLoginChecked, x => x.CorrelateById(context => context.Message.CorrelationId));
        Event(() => LoginAuditRecorded, x => x.CorrelateById(context => context.Message.CorrelationId));
        Event(() => LastLoginUpdated, x => x.CorrelateById(context => context.Message.CorrelationId));
        Event(() => LoginNotificationSent, x => x.CorrelateById(context => context.Message.CorrelationId));
        Event(() => LoginFailedEvent, x => x.CorrelateById(context => context.Message.CorrelationId));

        Initially(
            When(LoginStarted)
                .Then(context =>
                {
                    context.Saga.UserId = context.Message.UserId;
                    context.Saga.Email = context.Message.Email;
                    context.Saga.IpAddress = context.Message.IpAddress;
                    context.Saga.UserAgent = context.Message.UserAgent;
                    context.Saga.CreatedAt = DateTime.UtcNow;
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                    logger.LogInformation("Login saga started for UserId={UserId}", context.Message.UserId);
                })
                .Publish(context => new CheckSuspiciousLoginIntegrationEvent
                {
                    CorrelationId = context.Saga.CorrelationId,
                    UserId = context.Saga.UserId,
                    Email = context.Saga.Email,
                    IpAddress = context.Saga.IpAddress,
                    UserAgent = context.Saga.UserAgent
                })
                .TransitionTo(AwaitingSuspiciousCheck));

        During(AwaitingSuspiciousCheck,
            When(SuspiciousLoginChecked)
                .Then(context =>
                {
                    context.Saga.IsSuspicious = context.Message.IsSuspicious;
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                    logger.LogInformation("Suspicious check done for UserId={UserId}. IsSuspicious={IsSuspicious}",
                        context.Saga.UserId, context.Message.IsSuspicious);
                })
                .Publish(context => new RecordLoginAuditIntegrationEvent
                {
                    CorrelationId = context.Saga.CorrelationId,
                    UserId = context.Saga.UserId,
                    Email = context.Saga.Email,
                    IpAddress = context.Saga.IpAddress,
                    UserAgent = context.Saga.UserAgent,
                    IsSuspicious = context.Saga.IsSuspicious
                })
                .TransitionTo(AwaitingAuditRecorded));

        During(AwaitingAuditRecorded,
            When(LoginAuditRecorded)
                .Then(context =>
                {
                    context.Saga.AuditRecorded = true;
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                    logger.LogInformation("Login audit recorded for UserId={UserId}", context.Saga.UserId);
                })
                .Publish(context => new UpdateLastLoginIntegrationEvent
                {
                    CorrelationId = context.Saga.CorrelationId,
                    UserId = context.Saga.UserId,
                    Email = context.Saga.Email,
                    IpAddress = context.Saga.IpAddress,
                    UserAgent = context.Saga.UserAgent
                })
                .TransitionTo(AwaitingLastLoginUpdate));

        During(AwaitingLastLoginUpdate,
            When(LastLoginUpdated)
                .Then(context =>
                {
                    context.Saga.LastLoginUpdated = true;
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                    logger.LogInformation("Last login updated for UserId={UserId}", context.Saga.UserId);
                })
                .Publish(context => new SendLoginNotificationEmailIntegrationEvent
                {
                    CorrelationId = context.Saga.CorrelationId,
                    UserId = context.Saga.UserId,
                    Email = context.Saga.Email,
                    IpAddress = context.Saga.IpAddress,
                    UserAgent = context.Saga.UserAgent,
                    IsSuspicious = context.Saga.IsSuspicious
                })
                .TransitionTo(AwaitingLoginNotification));

        During(AwaitingLoginNotification,
            When(LoginNotificationSent)
                .Then(context =>
                {
                    context.Saga.NotificationSent = true;
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                    logger.LogInformation("Login notification sent for UserId={UserId}. Login saga completed.",
                        context.Saga.UserId);
                })
                .TransitionTo(LoginCompleted)
                .Finalize());

        DuringAny(
            When(LoginFailedEvent)
                .Then(context =>
                {
                    context.Saga.FailureReason = context.Message.FailureReason ?? "Unknown failure reason";
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                    logger.LogError("Login saga failed for UserId={UserId}. Reason: {Reason}",
                        context.Saga.UserId, context.Saga.FailureReason);
                })
                .TransitionTo(LoginFailed));

        SetCompletedWhenFinalized();
    }
}