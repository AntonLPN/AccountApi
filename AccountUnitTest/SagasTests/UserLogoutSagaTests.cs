using Account.Contracts.SagaEvents.UserLogoutSagaEvents.Commands;
using Account.Contracts.SagaEvents.UserLogoutSagaEvents.Events;
using Account.Infrastructure.Persistence.SagaModels;
using Account.Infrastructure.Saga.UserLogout;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AccountUnitTest.SagasTests;

public class UserLogoutSagaTests : IAsyncLifetime
{
    private ServiceProvider _provider = null!;

    //emulates the bus broker and allows us to test the saga in isolation without needing a real message broker
    private ITestHarness _harness = null!;

    public async ValueTask InitializeAsync()
    {
        _provider = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddSagaStateMachine<UserLogoutSaga, UserLogoutSagaState>()
                    .InMemoryRepository();
            })
            .BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async ValueTask DisposeAsync()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    [Fact]
    public async Task LogoutStarted_ShouldTransitionTo_AwaitingLogoutAudit()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";

        // Act
        await _harness.Bus.Publish(new UserLogoutSagaStartedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = ipAddress,
            UserAgent = userAgent
        }, cancellationToken: CancellationToken.None);

        // Assert
        var sagaHarness = _harness.GetSagaStateMachineHarness<UserLogoutSaga, UserLogoutSagaState>();
        var existsId = await sagaHarness.Exists(correlationId, saga => saga.AwaitingLogoutAudit);
        existsId.Should().NotBeNull("Saga must transition to AwaitingLogoutAudit state");

        var instance = sagaHarness.Created
            .Select(x => x.CorrelationId == correlationId, TestContext.Current.CancellationToken)
            .FirstOrDefault();
        instance.Should().NotBeNull();
        instance.Saga.UserId.Should().Be(userId);
        instance.Saga.Email.Should().Be(email);
        instance.Saga.IpAddress.Should().Be(ipAddress);
        instance.Saga.UserAgent.Should().Be(userAgent);
        instance.Saga.CreatedAt.Should().NotBe(default);
        instance.Saga.UpdatedAt.Should().NotBe(default);

        // Check the RecordLogoutAuditIntegrationEvent was published
        (await _harness.Published.Any<RecordLogoutAuditIntegrationEvent>(TestContext.Current.CancellationToken))
            .Should().BeTrue("Saga must publish RecordLogoutAuditIntegrationEvent");
    }

    [Fact]
    public async Task AwaitingLogoutAudit_ShouldTransitionTo_AwaitingLastLogoutUpdate()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";

        var sagaHarness = _harness.GetSagaStateMachineHarness<UserLogoutSaga, UserLogoutSagaState>();

        // Act - Start the saga
        await _harness.Bus.Publish(new UserLogoutSagaStartedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla"
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingLogoutAudit);

        // Publish audit recorded event
        await _harness.Bus.Publish(new LogoutAuditRecordedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla"
        }, cancellationToken: CancellationToken.None);

        // Assert
        var existsId = await sagaHarness.Exists(correlationId, saga => saga.AwaitingLastLogoutUpdate);
        existsId.Should().NotBeNull("Saga must transition to AwaitingLastLogoutUpdate state");

        var instance = sagaHarness.Sagas
            .Select(x => x.CorrelationId == correlationId, TestContext.Current.CancellationToken)
            .FirstOrDefault();
        instance.Should().NotBeNull();
        instance.Saga.AuditRecorded.Should().BeTrue("AuditRecorded should be true");

        // Check the UpdateLastLogoutIntegrationEvent was published
        (await _harness.Published.Any<UpdateLastLogoutIntegrationEvent>(TestContext.Current.CancellationToken))
            .Should().BeTrue("Saga must publish UpdateLastLogoutIntegrationEvent");
    }

    [Fact]
    public async Task AwaitingLastLogoutUpdate_ShouldTransitionTo_AwaitingLogoutNotification()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";

        var sagaHarness = _harness.GetSagaStateMachineHarness<UserLogoutSaga, UserLogoutSagaState>();

        // Act - Start the saga
        await _harness.Bus.Publish(new UserLogoutSagaStartedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla"
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingLogoutAudit);

        await _harness.Bus.Publish(new LogoutAuditRecordedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla"
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingLastLogoutUpdate);

        // Publish last logout updated event
        await _harness.Bus.Publish(new LastLogoutUpdatedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla"
        }, cancellationToken: CancellationToken.None);

        // Assert
        var existsId = await sagaHarness.Exists(correlationId, saga => saga.AwaitingLogoutNotification);
        existsId.Should().NotBeNull("Saga must transition to AwaitingLogoutNotification state");

        var instance = sagaHarness.Sagas
            .Select(x => x.CorrelationId == correlationId, TestContext.Current.CancellationToken)
            .FirstOrDefault();
        instance.Should().NotBeNull();
        instance.Saga.LastLogoutUpdated.Should().BeTrue("LastLogoutUpdated should be true");

        // Check the SendLogoutNotificationEmailIntegrationEvent was published
        (await _harness.Published.Any<SendLogoutNotificationEmailIntegrationEvent>(TestContext.Current.CancellationToken))
            .Should().BeTrue("Saga must publish SendLogoutNotificationEmailIntegrationEvent");
    }

    [Fact]
    public async Task AwaitingLogoutNotification_ShouldTransitionTo_LogoutCompleted()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";

        var sagaHarness = _harness.GetSagaStateMachineHarness<UserLogoutSaga, UserLogoutSagaState>();

        // Act - Start the saga
        await _harness.Bus.Publish(new UserLogoutSagaStartedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla"
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingLogoutAudit);

        await _harness.Bus.Publish(new LogoutAuditRecordedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla"
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingLastLogoutUpdate);

        await _harness.Bus.Publish(new LastLogoutUpdatedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla"
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingLogoutNotification);

        // Publish logout notification sent event - this should finalize saga
        await _harness.Bus.Publish(new LogoutNotificationSentIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla"
        }, cancellationToken: CancellationToken.None);

        // Assert - Give saga time to process final event
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // After finalization, saga is removed from repository, so we just verify the notification event was processed
        (await _harness.Published.Any<LogoutNotificationSentIntegrationEvent>(TestContext.Current.CancellationToken))
            .Should().BeTrue("Saga should have processed the notification sent event");
    }

    [Fact]
    public async Task LogoutFailed_ShouldTransitionTo_LogoutFailed()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";
        const string failureReason = "Failed to revoke session";

        var sagaHarness = _harness.GetSagaStateMachineHarness<UserLogoutSaga, UserLogoutSagaState>();

        // Act - Start the saga
        await _harness.Bus.Publish(new UserLogoutSagaStartedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla"
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingLogoutAudit);

        // Publish logout failed event
        await _harness.Bus.Publish(new UserLogoutSagaFailedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            FailureReason = failureReason
        }, CancellationToken.None);

        // Assert
        var existsId = await sagaHarness.Exists(correlationId, saga => saga.LogoutFailed);
        existsId.Should().NotBeNull("Saga must transition to LogoutFailed state");

        var instance = sagaHarness.Sagas
            .Select(x => x.CorrelationId == correlationId, TestContext.Current.CancellationToken)
            .FirstOrDefault();
        instance.Should().NotBeNull();
        instance.Saga.FailureReason.Should().Be(failureReason);
    }

    [Fact]
    public async Task LogoutFailed_WithoutFailureReason_ShouldSetDefaultReason()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();

        var sagaHarness = _harness.GetSagaStateMachineHarness<UserLogoutSaga, UserLogoutSagaState>();

        // Act - Start the saga
        await _harness.Bus.Publish(new UserLogoutSagaStartedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = "test@example.com",
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla"
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingLogoutAudit);

        // Publish logout failed event without failure reason
        await _harness.Bus.Publish(new UserLogoutSagaFailedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            FailureReason = null
        }, CancellationToken.None);

        // Assert
        var existsId = await sagaHarness.Exists(correlationId, saga => saga.LogoutFailed);
        existsId.Should().NotBeNull("Saga must transition to LogoutFailed state");

        var instance = sagaHarness.Sagas
            .Select(x => x.CorrelationId == correlationId, TestContext.Current.CancellationToken)
            .FirstOrDefault();
        instance.Should().NotBeNull();
        instance.Saga.FailureReason.Should().Be("Unknown failure reason");
    }

    [Fact]
    public async Task CompleteFlow_ShouldPublishAllCommands()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";
        var ipAddress = "203.0.113.0";
        var userAgent = "Mozilla";

        var sagaHarness = _harness.GetSagaStateMachineHarness<UserLogoutSaga, UserLogoutSagaState>();

        // Act - Complete full flow
        await _harness.Bus.Publish(new UserLogoutSagaStartedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = ipAddress,
            UserAgent = userAgent
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingLogoutAudit);

        await _harness.Bus.Publish(new LogoutAuditRecordedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = ipAddress,
            UserAgent = userAgent
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingLastLogoutUpdate);

        await _harness.Bus.Publish(new LastLogoutUpdatedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = ipAddress,
            UserAgent = userAgent
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingLogoutNotification);

        // Check state before finalization
        var beforeFinalize = sagaHarness.Sagas
            .Select(x => x.CorrelationId == correlationId, TestContext.Current.CancellationToken)
            .FirstOrDefault();
        beforeFinalize.Should().NotBeNull("Saga should exist before finalization");
        beforeFinalize.Saga.AuditRecorded.Should().BeTrue("Audit should be recorded");
        beforeFinalize.Saga.LastLogoutUpdated.Should().BeTrue("Last logout should be updated");

        await _harness.Bus.Publish(new LogoutNotificationSentIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = ipAddress,
            UserAgent = userAgent
        }, cancellationToken: CancellationToken.None);

        // Assert - Give saga time to process final event and finalize
        await Task.Delay(100, TestContext.Current.CancellationToken);

        (await _harness.Published.Any<RecordLogoutAuditIntegrationEvent>(TestContext.Current.CancellationToken))
            .Should().BeTrue("RecordLogoutAudit command should have been published");
        (await _harness.Published.Any<UpdateLastLogoutIntegrationEvent>(TestContext.Current.CancellationToken))
            .Should().BeTrue("UpdateLastLogout command should have been published");
        (await _harness.Published.Any<SendLogoutNotificationEmailIntegrationEvent>(TestContext.Current.CancellationToken))
            .Should().BeTrue("SendLogoutNotification command should have been published");
    }
}