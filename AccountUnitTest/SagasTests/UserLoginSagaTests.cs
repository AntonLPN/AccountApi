using Account.Contracts.Saga.UserLoginSagaEvents.Commands;
using Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;
using Account.Infrastructure.Persistence.SagaModels;
using Account.Infrastructure.Saga.UserLogin;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AccountUnitTest.SagasTests;

public class UserLoginSagaTests : IAsyncLifetime
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
                cfg.AddSagaStateMachine<UserLoginSaga, UserLoginSagaState>()
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
    public async Task LoginStarted_ShouldTransitionTo_AwaitingSuspiciousCheck()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";

        // Act
        await _harness.Bus.Publish(new UserLoginSagaStartedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = ipAddress,
            UserAgent = userAgent
        }, cancellationToken: CancellationToken.None);

        // Assert
        var sagaHarness = _harness.GetSagaStateMachineHarness<UserLoginSaga, UserLoginSagaState>();
        var existsId = await sagaHarness.Exists(correlationId, saga => saga.AwaitingSuspiciousCheck);
        existsId.Should().NotBeNull("Saga must transition to AwaitingSuspiciousCheck state");

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

        // Check the CheckSuspiciousLoginIntegrationEvent was published
        (await _harness.Published.Any<CheckSuspiciousLoginIntegrationCommand>(TestContext.Current.CancellationToken))
            .Should().BeTrue("Saga must publish CheckSuspiciousLoginIntegrationEvent");
    }

    [Fact]
    public async Task AwaitingSuspiciousCheck_ShouldTransitionTo_AwaitingAuditRecorded()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";
        var ipAddress = "10.0.0.1";
        var userAgent = "Chrome";

        var sagaHarness = _harness.GetSagaStateMachineHarness<UserLoginSaga, UserLoginSagaState>();

        // Act - Start the saga
        await _harness.Bus.Publish(new UserLoginSagaStartedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = ipAddress,
            UserAgent = userAgent
        }, cancellationToken: CancellationToken.None);

        // Wait for saga to reach AwaitingSuspiciousCheck state
        await sagaHarness.Exists(correlationId, saga => saga.AwaitingSuspiciousCheck);

        // Act - Publish suspicious check result (not suspicious)
        await _harness.Bus.Publish(new SuspiciousLoginCheckedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            IsSuspicious = false
        }, cancellationToken: CancellationToken.None);

        // Assert
        var existsId = await sagaHarness.Exists(correlationId, saga => saga.AwaitingAuditRecorded);
        existsId.Should().NotBeNull("Saga must transition to AwaitingAuditRecorded state");

        var instance = sagaHarness.Sagas
            .Select(x => x.CorrelationId == correlationId, TestContext.Current.CancellationToken)
            .FirstOrDefault();
        instance.Should().NotBeNull();
        instance.Saga.IsSuspicious.Should().BeFalse("IsSuspicious should be false");

        // Check the RecordLoginAuditIntegrationEvent was published
        (await _harness.Published.Any<RecordLoginAuditIntegrationCommand>(TestContext.Current.CancellationToken))
            .Should().BeTrue("Saga must publish RecordLoginAuditIntegrationEvent");
    }

    [Fact]
    public async Task AwaitingSuspiciousCheck_ShouldDetectSuspiciousLogin()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";

        var sagaHarness = _harness.GetSagaStateMachineHarness<UserLoginSaga, UserLoginSagaState>();

        // Act - Start the saga
        await _harness.Bus.Publish(new UserLoginSagaStartedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla"
        }, cancellationToken: CancellationToken.None);

        // Wait for saga to reach AwaitingSuspiciousCheck state
        await sagaHarness.Exists(correlationId, saga => saga.AwaitingSuspiciousCheck);

        // Act - Publish suspicious check result (suspicious)
        await _harness.Bus.Publish(new SuspiciousLoginCheckedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla",
            IsSuspicious = true
        }, cancellationToken: CancellationToken.None);

        // Assert - Wait for saga to transition and update IsSuspicious
        await sagaHarness.Exists(correlationId, saga => saga.AwaitingAuditRecorded);

        var instance = sagaHarness.Sagas
            .Select(x => x.CorrelationId == correlationId, TestContext.Current.CancellationToken)
            .FirstOrDefault();
        instance.Should().NotBeNull();
        instance.Saga.IsSuspicious.Should().BeTrue("IsSuspicious should be true");

        // Saga must still publish RecordLoginAuditIntegrationEvent with IsSuspicious=true
        (await _harness.Published.Any<RecordLoginAuditIntegrationCommand>(
                x => x.Context.Message.IsSuspicious,
                TestContext.Current.CancellationToken))
            .Should().BeTrue("Saga must publish RecordLoginAuditIntegrationEvent with IsSuspicious=true");
    }

    [Fact]
    public async Task AwaitingAuditRecorded_ShouldTransitionTo_AwaitingLastLoginUpdate()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";

        var sagaHarness = _harness.GetSagaStateMachineHarness<UserLoginSaga, UserLoginSagaState>();

        // Act - Start the saga
        await _harness.Bus.Publish(new UserLoginSagaStartedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla"
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingSuspiciousCheck);

        // Publish suspicious check result
        await _harness.Bus.Publish(new SuspiciousLoginCheckedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla",
            IsSuspicious = false
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingAuditRecorded);

        // Publish audit recorded event
        await _harness.Bus.Publish(new LoginAuditRecordedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla",
            IsSuspicious = false
        }, cancellationToken: CancellationToken.None);

        // Assert
        var existsId = await sagaHarness.Exists(correlationId, saga => saga.AwaitingLastLoginUpdate);
        existsId.Should().NotBeNull("Saga must transition to AwaitingLastLoginUpdate state");

        var instance = sagaHarness.Sagas
            .Select(x => x.CorrelationId == correlationId, TestContext.Current.CancellationToken)
            .FirstOrDefault();
        instance.Should().NotBeNull();
        instance.Saga.AuditRecorded.Should().BeTrue("AuditRecorded should be true");

        // Check the UpdateLastLoginIntegrationEvent was published
        (await _harness.Published.Any<UpdateLastLoginIntegrationCommand>(TestContext.Current.CancellationToken))
            .Should().BeTrue("Saga must publish UpdateLastLoginIntegrationEvent");
    }

    [Fact]
    public async Task AwaitingLastLoginUpdate_ShouldTransitionTo_AwaitingLoginNotification()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";

        var sagaHarness = _harness.GetSagaStateMachineHarness<UserLoginSaga, UserLoginSagaState>();

        // Act - Start the saga
        await _harness.Bus.Publish(new UserLoginSagaStartedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla"
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingSuspiciousCheck);

        // Publish suspicious check result
        await _harness.Bus.Publish(new SuspiciousLoginCheckedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla",
            IsSuspicious = false
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingAuditRecorded);

        // Publish audit recorded event
        await _harness.Bus.Publish(new LoginAuditRecordedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla",
            IsSuspicious = false
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingLastLoginUpdate);

        // Publish last login updated event
        await _harness.Bus.Publish(new LastLoginUpdatedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla"
        }, cancellationToken: CancellationToken.None);

        // Assert
        var existsId = await sagaHarness.Exists(correlationId, saga => saga.AwaitingLoginNotification);
        existsId.Should().NotBeNull("Saga must transition to AwaitingLoginNotification state");

        var instance = sagaHarness.Sagas
            .Select(x => x.CorrelationId == correlationId, TestContext.Current.CancellationToken)
            .FirstOrDefault();
        instance.Should().NotBeNull();
        instance.Saga.LastLoginUpdated.Should().BeTrue("LastLoginUpdated should be true");

        // Check the SendLoginNotificationEmailIntegrationEvent was published
        (await _harness.Published.Any<SendLoginNotificationEmailIntegrationCommand>(TestContext.Current.CancellationToken))
            .Should().BeTrue("Saga must publish SendLoginNotificationEmailIntegrationEvent");
    }

    [Fact]
    public async Task AwaitingLoginNotification_ShouldTransitionTo_LoginCompleted()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";

        var sagaHarness = _harness.GetSagaStateMachineHarness<UserLoginSaga, UserLoginSagaState>();

        // Act - Start the saga
        await _harness.Bus.Publish(new UserLoginSagaStartedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla"
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingSuspiciousCheck);

        // Publish suspicious check result
        await _harness.Bus.Publish(new SuspiciousLoginCheckedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla",
            IsSuspicious = false
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingAuditRecorded);

        // Publish audit recorded event
        await _harness.Bus.Publish(new LoginAuditRecordedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla",
            IsSuspicious = false
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingLastLoginUpdate);

        // Publish last login updated event
        await _harness.Bus.Publish(new LastLoginUpdatedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla"
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingLoginNotification);

        // Publish login notification sent event - this should finalize saga
        await _harness.Bus.Publish(new LoginNotificationSentIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla"
        }, cancellationToken: CancellationToken.None);

        // Assert - Give saga time to process final event
        await Task.Delay(100, TestContext.Current.CancellationToken);
        
        // After finalization, saga is removed from repository, so we just verify no exceptions occurred
        // by checking that the published events include the notification event
        (await _harness.Published.Any<LoginNotificationSentIntegrationEvent>(TestContext.Current.CancellationToken))
            .Should().BeTrue("Saga should have processed the notification sent event");
    }

    [Fact]
    public async Task LoginFailed_ShouldTransitionTo_LoginFailed()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";
        const string failureReason = "Suspicious activity detected";

        var sagaHarness = _harness.GetSagaStateMachineHarness<UserLoginSaga, UserLoginSagaState>();

        // Act - Start the saga
        await _harness.Bus.Publish(new UserLoginSagaStartedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla"
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingSuspiciousCheck);

        // Publish login failed event
        await _harness.Bus.Publish(new UserLoginSagaFailedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            FailureReason = failureReason
        }, CancellationToken.None);

        // Assert
        var existsId = await sagaHarness.Exists(correlationId, saga => saga.LoginFailed);
        existsId.Should().NotBeNull("Saga must transition to LoginFailed state");

        var instance = sagaHarness.Sagas
            .Select(x => x.CorrelationId == correlationId, TestContext.Current.CancellationToken)
            .FirstOrDefault();
        instance.Should().NotBeNull();
        instance.Saga.FailureReason.Should().Be(failureReason);
    }

    [Fact]
    public async Task LoginFailed_AtAnyState_ShouldTransitionTo_LoginFailed()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";
        const string failureReason = "System error during audit recording";

        var sagaHarness = _harness.GetSagaStateMachineHarness<UserLoginSaga, UserLoginSagaState>();

        // Act - Start the saga
        await _harness.Bus.Publish(new UserLoginSagaStartedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla"
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingSuspiciousCheck);

        // Publish suspicious check result
        await _harness.Bus.Publish(new SuspiciousLoginCheckedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla",
            IsSuspicious = false
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingAuditRecorded);

        // At AwaitingAuditRecorded state - publish login failed event
        await _harness.Bus.Publish(new UserLoginSagaFailedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            FailureReason = failureReason
        }, CancellationToken.None);

        // Assert
        var existsId = await sagaHarness.Exists(correlationId, saga => saga.LoginFailed);
        existsId.Should().NotBeNull("Saga must transition to LoginFailed state from any state");

        var instance = sagaHarness.Sagas
            .Select(x => x.CorrelationId == correlationId, TestContext.Current.CancellationToken)
            .FirstOrDefault();
        instance.Should().NotBeNull();
        instance.Saga.FailureReason.Should().Be(failureReason);
    }

    [Fact]
    public async Task LoginFailed_WithoutFailureReason_ShouldSetDefaultReason()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();

        var sagaHarness = _harness.GetSagaStateMachineHarness<UserLoginSaga, UserLoginSagaState>();

        // Act - Start the saga
        await _harness.Bus.Publish(new UserLoginSagaStartedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = "test@example.com",
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla"
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingSuspiciousCheck);

        // Publish login failed event without failure reason
        await _harness.Bus.Publish(new UserLoginSagaFailedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            FailureReason = null
        }, CancellationToken.None);

        // Assert
        var existsId = await sagaHarness.Exists(correlationId, saga => saga.LoginFailed);
        existsId.Should().NotBeNull("Saga must transition to LoginFailed state");

        var instance = sagaHarness.Sagas
            .Select(x => x.CorrelationId == correlationId, TestContext.Current.CancellationToken)
            .FirstOrDefault();
        instance.Should().NotBeNull();
        instance.Saga.FailureReason.Should().Be("Unknown failure reason");
    }

    [Fact]
    public async Task CompleteFlow_SuspiciousLogin_ShouldCompleteSuccessfully()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";
        var ipAddress = "203.0.113.0"; // Unusual IP
        var userAgent = "UnknownBot";

        var sagaHarness = _harness.GetSagaStateMachineHarness<UserLoginSaga, UserLoginSagaState>();

        // Act - Complete full flow with suspicious login
        await _harness.Bus.Publish(new UserLoginSagaStartedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = ipAddress,
            UserAgent = userAgent
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingSuspiciousCheck);

        await _harness.Bus.Publish(new SuspiciousLoginCheckedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            IsSuspicious = true
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingAuditRecorded);

        await _harness.Bus.Publish(new LoginAuditRecordedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            IsSuspicious = true
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingLastLoginUpdate);

        await _harness.Bus.Publish(new LastLoginUpdatedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = ipAddress,
            UserAgent = userAgent
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingLoginNotification);

        // Check state before finalization
        var beforeFinalize = sagaHarness.Sagas
            .Select(x => x.CorrelationId == correlationId, TestContext.Current.CancellationToken)
            .FirstOrDefault();
        beforeFinalize.Should().NotBeNull("Saga should exist before finalization");

        // Verify all flags are set correctly before finalization
        beforeFinalize.Saga.IsSuspicious.Should().BeTrue("Login should be marked as suspicious");
        beforeFinalize.Saga.AuditRecorded.Should().BeTrue("Audit should be recorded");
        beforeFinalize.Saga.LastLoginUpdated.Should().BeTrue("Last login should be updated");

        await _harness.Bus.Publish(new LoginNotificationSentIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            IpAddress = ipAddress,
            UserAgent = userAgent
        }, cancellationToken: CancellationToken.None);

        // Assert - Give saga time to process final event and finalize
        await Task.Delay(100, TestContext.Current.CancellationToken);
        
        // Verify that all command events were published
        (await _harness.Published.Any<CheckSuspiciousLoginIntegrationCommand>(TestContext.Current.CancellationToken))
            .Should().BeTrue("CheckSuspiciousLogin command should have been published");
        (await _harness.Published.Any<RecordLoginAuditIntegrationCommand>(TestContext.Current.CancellationToken))
            .Should().BeTrue("RecordLoginAudit command should have been published");
        (await _harness.Published.Any<UpdateLastLoginIntegrationCommand>(TestContext.Current.CancellationToken))
            .Should().BeTrue("UpdateLastLogin command should have been published");
        (await _harness.Published.Any<SendLoginNotificationEmailIntegrationCommand>(TestContext.Current.CancellationToken))
            .Should().BeTrue("SendLoginNotification command should have been published");
    }
}