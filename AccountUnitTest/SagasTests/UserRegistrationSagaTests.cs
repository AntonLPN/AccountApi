using Account.Contracts.SagaEvents.UserRegisterSagaEvents.Commands;
using Account.Contracts.SagaEvents.UserRegisterSagaEvents.Events;
using Account.Infrastructure.Persistence.SagaModels;
using Account.Infrastructure.Saga.UserRegister;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AccountUnitTest.SagasTests;

public class UserRegistrationSagaTests : IAsyncLifetime
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
                cfg.AddSagaStateMachine<UserRegistrationSaga, UserRegistrationSagaState>()
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
    public async Task RegistrationStarted_ShouldTransitionTo_AwaitingEmailConfirmation()
    {
        var correlationId = Guid.NewGuid();
        string email = "test@example.com";
        // Act
        await _harness.Bus.Publish(new UserSagaStartedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = Guid.NewGuid().ToString(),
            Email = email,
            ApiKey = "api_key"
        }, cancellationToken: CancellationToken.None);
        // Assert
        var sagaHarness = _harness.GetSagaStateMachineHarness<UserRegistrationSaga, UserRegistrationSagaState>();
        var existsId = await sagaHarness.Exists(correlationId, saga => saga.AwaitingWelcomeEmailSent);
        existsId.Should().NotBeNull("Saga must go to state AwaitingEmailConfirmation");

        var instance = sagaHarness.Created
            .Select(x => x.CorrelationId == correlationId, TestContext.Current.CancellationToken)
            .FirstOrDefault();
        instance.Should().NotBeNull();
        instance.Saga.Email.Should().Be(email);

        // Check the SendEmailConfirmationCommandIntegrationEvent was published
        (await _harness.Published.Any<SendWelcomeEmailIntegrationEvent>(TestContext.Current
                .CancellationToken))
            .Should().BeTrue();
    }

    [Fact]
    public async Task AwaitingEmailConfirmation_ShouldTransitionTo_AwaitingProfileInitialization()
    {
        var correlationId = Guid.NewGuid();
        string email = "test@mail.com";
        string userId = Guid.NewGuid().ToString();
        // Act
        await _harness.Bus.Publish(new UserSagaStartedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            ApiKey = "api_key"
        }, cancellationToken: CancellationToken.None);

        await _harness.Bus.Publish(new WelcomeEmailSentIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            ApiKey = "api_key"
        }, cancellationToken: CancellationToken.None);

        // Assert
        var sagaHarness = _harness.GetSagaStateMachineHarness<UserRegistrationSaga, UserRegistrationSagaState>();
        var existsId = await sagaHarness.Exists(correlationId, saga => saga.AwaitingProfileInitialization);
        existsId.Should().NotBeNull("Saga must go to state AwaitingProfileInitialization");
        var instance = sagaHarness.Created
            .Select(x => x.CorrelationId == correlationId, TestContext.Current.CancellationToken)
            .FirstOrDefault();
        instance.Should().NotBeNull();
        instance.Saga.Email.Should().Be(email);
    }

    [Fact]
    public async Task RegistrationFailed_ShouldTransitTo_Failde()
    {
        var correlationId = Guid.NewGuid();
        string email = "test@mail.com";
        string userId = Guid.NewGuid().ToString();
        const string failureReason = "Registration failed";
        // Act
        var sagaHarness = _harness.GetSagaStateMachineHarness<UserRegistrationSaga, UserRegistrationSagaState>();
        await _harness.Bus.Publish(new UserSagaStartedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            Email = email,
            ApiKey = "api_key"
        }, cancellationToken: CancellationToken.None);

        await sagaHarness.Exists(correlationId, saga => saga.AwaitingWelcomeEmailSent);
        await _harness.Bus.Publish(new UserRegistrationSagaFailedIntegrationEvent()
        {
            CorrelationId = correlationId,
            UserId = userId,
            FailureReason = "Registration failed"
        }, CancellationToken.None);

        //Asserts 
        // Assert — saga transitioned to RegistrationFailed
        var existsId = await sagaHarness.Exists(correlationId, saga => saga.RegistrationFailed);
        existsId.Should().NotBeNull("Saga must transition to RegistrationFailed state");

        var instance = sagaHarness.Sagas
            .Select(x => x.CorrelationId == correlationId, TestContext.Current.CancellationToken)
            .FirstOrDefault();
        instance.Should().NotBeNull();
        instance.Saga.FailureReason.Should().Be(failureReason);

        (await _harness.Published.Any<UserRegistrationSagaFailedIntegrationEvent>(
                x => x.Context.Message.CorrelationId == correlationId,
                TestContext.Current.CancellationToken))
            .Should().BeTrue("Saga must re-publish UserRegistrationSagaFailedIntegrationEvent");
    }
}