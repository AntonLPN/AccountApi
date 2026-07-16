using Account.Application.Features.Account.ProviderLogin;
using Account.Application.Interfaces;
using Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;
using Account.Domain.Entities;
using Account.Domain.Enums;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Domain.Repositories;
using Ardalis.Result;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountUnitTest.HandlerTests;

public class ProviderLoginHandlerTests
{
    private readonly Mock<ILogger<ProviderLoginHandler>> _logger = new();
    private readonly Mock<IProviderValidator> _providerValidator = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IApiKeyRepository> _apiKeyRepository = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAuthService> _authService = new();

    private ProviderLoginHandler CreateSut()
    {
        return new ProviderLoginHandler(
            _logger.Object,
            _providerValidator.Object,
            _userRepository.Object,
            _apiKeyRepository.Object,
            _publishEndpoint.Object,
            _unitOfWork.Object,
            _authService.Object);
    }

    private static ProviderLoginCommand CreateCommand(
        string providerToken = "google-token-123",
        AuthProviders provider = AuthProviders.Google,
        string? ipAddress = "192.168.1.1",
        string? userAgent = "Mozilla/5.0")
        => new(providerToken, provider, ipAddress, userAgent);

    private static AppUser CreateUser(string id = "user123", string email = "test@mail.com")
    {
        return new AppUser
        {
            Id = id,
            Email = email,
            UserName = email,
            PasswordHash = "hash",
            EmailConfirmed = true
        };
    }

    private static TokenResponse CreateTokenResponse(
        string accessToken = "access_token",
        string refreshToken = "refresh_token",
        string tokenType = "Bearer",
        int expiresIn = 3600,
        string scope = "scope")
    {
        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = tokenType,
            ExpiresIn = expiresIn,
            Scope = scope
        };
    }

    [Fact]
    public async Task Handle_WhenProviderValidationFails_ReturnsNull()
    {
        // Arrange
        var sut = CreateSut();
        var command = CreateCommand();

        _providerValidator
            .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(command.Provider, command.ProviderToken))
            .ReturnsAsync((string?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => await sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsUnauthorized()
    {
        // Arrange
        var sut = CreateSut();
        var command = CreateCommand();
        const string email = "test@mail.com";

        _providerValidator
            .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(command.Provider, command.ProviderToken))
            .ReturnsAsync(email);

        _userRepository
            .Setup(x => x.GetUserByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        _publishEndpoint.Verify(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAuthServiceReturnsNull_ThrowsException()
    {
        // Arrange
        var sut = CreateSut();
        var command = CreateCommand();
        var user = CreateUser();
        const string email = "test@mail.com";

        _providerValidator
            .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(command.Provider, command.ProviderToken))
            .ReturnsAsync(email);

        _userRepository
            .Setup(x => x.GetUserByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _authService
            .Setup(x => x.LoginAsync(email))
            .ReturnsAsync((TokenResponse?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenLoginSucceeds_ReturnsSuccessWithTokenAndApiKey()
    {
        // Arrange
        var sut = CreateSut();
        var command = CreateCommand();
        var user = CreateUser();
        var tokenResponse = CreateTokenResponse();
        const string apiKey = "api-key-123";
        const string email = "test@mail.com";

        _providerValidator
            .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(command.Provider, command.ProviderToken))
            .ReturnsAsync(email);

        _userRepository
            .Setup(x => x.GetUserByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _authService
            .Setup(x => x.LoginAsync(email))
            .ReturnsAsync(tokenResponse);

        _apiKeyRepository
            .Setup(x => x.GetApiKeyByUserIdAsync(user.Id))
            .ReturnsAsync(apiKey);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(apiKey, result.Value.ApiKey);
        Assert.Same(tokenResponse, result.Value.Token);
    }

    [Fact]
    public async Task Handle_WhenApiKeyIsNull_ReturnsSuccessWithEmptyApiKey()
    {
        // Arrange
        var sut = CreateSut();
        var command = CreateCommand();
        var user = CreateUser();
        var tokenResponse = CreateTokenResponse();
        const string email = "test@mail.com";

        _providerValidator
            .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(command.Provider, command.ProviderToken))
            .ReturnsAsync(email);

        _userRepository
            .Setup(x => x.GetUserByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _authService
            .Setup(x => x.LoginAsync(email))
            .ReturnsAsync(tokenResponse);

        _apiKeyRepository
            .Setup(x => x.GetApiKeyByUserIdAsync(user.Id))
            .ReturnsAsync((string?)null);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("", result.Value.ApiKey);
    }

    [Fact]
    public async Task Handle_WhenLoginSucceeds_PublishesSagaStartedEventWithUserDetails()
    {
        // Arrange
        var sut = CreateSut();
        var command = CreateCommand();
        var user = CreateUser();
        const string email = "test@mail.com";

        _providerValidator
            .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(command.Provider, command.ProviderToken))
            .ReturnsAsync(email);

        _userRepository
            .Setup(x => x.GetUserByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _authService
            .Setup(x => x.LoginAsync(email))
            .ReturnsAsync(CreateTokenResponse());

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        _publishEndpoint.Verify(x => x.Publish(
            It.Is<UserLoginSagaStartedIntegrationEvent>(e =>
                e.CorrelationId != Guid.Empty &&
                e.UserId == user.Id &&
                e.Email == user.Email &&
                e.IpAddress == command.IpAddress &&
                e.UserAgent == command.UserAgent),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenLoginSucceeds_SavesChangesForSaga()
    {
        // Arrange
        var sut = CreateSut();
        var command = CreateCommand();
        var user = CreateUser();
        const string email = "test@mail.com";

        _providerValidator
            .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(command.Provider, command.ProviderToken))
            .ReturnsAsync(email);

        _userRepository
            .Setup(x => x.GetUserByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _authService
            .Setup(x => x.LoginAsync(email))
            .ReturnsAsync(CreateTokenResponse());

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken()
    {
        // Arrange
        var sut = CreateSut();
        var command = CreateCommand();
        var user = CreateUser();
        const string email = "test@mail.com";
        using var cts = new CancellationTokenSource();

        _providerValidator
            .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(command.Provider, command.ProviderToken))
            .ReturnsAsync(email);

        _userRepository
            .Setup(x => x.GetUserByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _authService
            .Setup(x => x.LoginAsync(email))
            .ReturnsAsync(CreateTokenResponse());

        // Act
        await sut.Handle(command, cts.Token);

        // Assert
        _userRepository.Verify(x => x.GetUserByEmailAsync(email, cts.Token), Times.Once);
        _publishEndpoint.Verify(x => x.Publish(It.IsAny<UserLoginSagaStartedIntegrationEvent>(), cts.Token), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(cts.Token), Times.Once);
    }
}

