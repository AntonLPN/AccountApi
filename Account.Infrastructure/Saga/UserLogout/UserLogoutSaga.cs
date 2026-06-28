using System.Diagnostics.CodeAnalysis;
using Account.Contracts.Saga.UserLogoutSagaEvents.Commands;
using Account.Contracts.Saga.UserLogoutSagaEvents.Events;
using Account.Contracts.SagaEvents.UserLogoutSagaEvents.Commands;
using Account.Contracts.SagaEvents.UserLogoutSagaEvents.Events;
using Account.Infrastructure.Persistence.SagaModels;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Saga.UserLogout;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class UserLogoutSaga : MassTransitStateMachine<UserLogoutSagaState>
{
    public State AwaitingLogoutAudit { get; private set; } = null!;
    public State AwaitingLastLogoutUpdate { get; private set; } = null!;
    public State AwaitingLogoutNotification { get; private set; } = null!;
    public State LogoutCompleted { get; private set; } = null!;
    public State LogoutFailed { get; private set; } = null!;


    public Event<UserLogoutSagaStartedIntegrationEvent> LogoutStarted { get; private set; } = null!;
    public Event<LogoutAuditRecordedIntegrationEvent> LogoutAuditRecorded { get; private set; } = null!;
    public Event<LastLogoutUpdatedIntegrationEvent> LastLogoutUpdated { get; private set; } = null!;
    public Event<LogoutNotificationSentIntegrationEvent> LogoutNotificationSent { get; private set; } = null!;
    public Event<UserLogoutSagaFailedIntegrationEvent> LogoutFailedEvent { get; private set; } = null!;

    public UserLogoutSaga(ILogger<UserLogoutSaga> logger)
    {
        InstanceState(x => x.CurrentState);

        Event(() => LogoutStarted, x => x.CorrelateById(context => context.Message.CorrelationId));
        Event(() => LogoutAuditRecorded, x => x.CorrelateById(context => context.Message.CorrelationId));
        Event(() => LastLogoutUpdated, x => x.CorrelateById(context => context.Message.CorrelationId));
        Event(() => LogoutNotificationSent, x => x.CorrelateById(context => context.Message.CorrelationId));
        Event(() => LogoutFailedEvent, x => x.CorrelateById(context => context.Message.CorrelationId));

        Initially(
            When(LogoutStarted)
                .Then(context =>
                {
                    context.Saga.UserId = context.Message.UserId;
                    context.Saga.Email = context.Message.Email;
                    context.Saga.IpAddress = context.Message.IpAddress;
                    context.Saga.UserAgent = context.Message.UserAgent;
                    context.Saga.CreatedAt = DateTime.UtcNow;
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                    logger.LogInformation("Logout saga started for UserId={UserId}", context.Message.UserId);
                })
                .Publish(context => new RecordLogoutAuditIntegrationCommand
                {
                    CorrelationId = context.Saga.CorrelationId,
                    UserId = context.Saga.UserId,
                    Email = context.Saga.Email,
                    IpAddress = context.Saga.IpAddress,
                    UserAgent = context.Saga.UserAgent
                })
                .TransitionTo(AwaitingLogoutAudit));

        During(AwaitingLogoutAudit,
            When(LogoutAuditRecorded)
                .Then(context =>
                {
                    context.Saga.AuditRecorded = true;
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                    logger.LogInformation("Logout audit recorded for UserId={UserId}", context.Saga.UserId);
                })
                .Publish(context => new UpdateLastLogoutIntegrationCommand
                {
                    CorrelationId = context.Saga.CorrelationId,
                    UserId = context.Saga.UserId,
                    Email = context.Saga.Email,
                    IpAddress = context.Saga.IpAddress,
                    UserAgent = context.Saga.UserAgent
                })
                .TransitionTo(AwaitingLastLogoutUpdate));

        During(AwaitingLastLogoutUpdate,
            When(LastLogoutUpdated)
                .Then(context =>
                {
                    context.Saga.LastLogoutUpdated = true;
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                    logger.LogInformation("Last logout updated for UserId={UserId}", context.Saga.UserId);
                })
                .Publish(context => new SendLogoutNotificationEmailIntegrationCommand
                {
                    CorrelationId = context.Saga.CorrelationId,
                    UserId = context.Saga.UserId,
                    Email = context.Saga.Email,
                    IpAddress = context.Saga.IpAddress,
                    UserAgent = context.Saga.UserAgent
                })
                .TransitionTo(AwaitingLogoutNotification));

        During(AwaitingLogoutNotification,
            When(LogoutNotificationSent)
                .Then(context =>
                {
                    context.Saga.NotificationSent = true;
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                    logger.LogInformation("Logout notification sent for UserId={UserId}. Logout saga completed.",
                        context.Saga.UserId);
                })
                .TransitionTo(LogoutCompleted)
                .Finalize());

        DuringAny(
            When(LogoutFailedEvent)
                .Then(context =>
                {
                    context.Saga.FailureReason = context.Message.FailureReason ?? "Unknown failure reason";
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                    logger.LogError("Logout saga failed for UserId={UserId}. Reason: {Reason}",
                        context.Saga.UserId, context.Saga.FailureReason);
                })
                .TransitionTo(LogoutFailed));

        SetCompletedWhenFinalized();//if you need delete from Db 
    }
}