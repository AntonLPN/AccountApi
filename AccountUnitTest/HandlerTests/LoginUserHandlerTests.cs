using Account.Application.Features.Account.Login;
using Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;
using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Domain.Repositories;
using Ardalis.Result;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountUnitTest.HandlerTests;

public class LoginUserHandlerTests
{
    private readonly Mock<ILogger<LoginUserHandler>> _logger = new();
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IApiKeyRepository> _apiKeyRepository = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly Mock<ICryptography> _cryptographyService = new();
    private readonly Mock<IOtpSessionRepository> _otpSessionsRepository = new();

    private LoginUserHandler CreateSut()
    {
        return new LoginUserHandler(
            _logger.Object,
            _authService.Object,
            _unitOfWork.Object,
            _userRepository.Object,
            _apiKeyRepository.Object,
            _publishEndpoint.Object,
            _cryptographyService.Object,
            _otpSessionsRepository.Object);
    }

    private static LoginCommand CreateCommand(
        string email = "test@mail.com",
        string password = "123Avc_!@#$%^&*()_+",
        string? ipAddress = "192.168.1.1",
        string? userAgent = "Mozilla/5.0")
        => new(email, password, ipAddress, userAgent);

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
    public async Task Handle_WhenAuthServiceReturnsNull_ReturnsUnauthorized()
    {
        // Arrange
        var sut = CreateSut();
        var command = CreateCommand();
        
        _authService
            .Setup(x => x.LoginAsync(command.Email, command.Password))
            .ReturnsAsync((TokenResponse?)null);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        _userRepository.Verify(x => x.GetUserByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _publishEndpoint.Verify(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsUnauthorized()
    {
        // Arrange
        var sut = CreateSut();
        var command = CreateCommand();
        var tokenResponse = CreateTokenResponse();

        _authService
            .Setup(x => x.LoginAsync(command.Email, command.Password))
            .ReturnsAsync(tokenResponse);

        _userRepository
            .Setup(x => x.GetUserByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        _apiKeyRepository.Verify(x => x.GetApiKeyByUserIdAsync(It.IsAny<string>()), Times.Never);
        _publishEndpoint.Verify(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
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

        _authService
            .Setup(x => x.LoginAsync(command.Email, command.Password))
            .ReturnsAsync(tokenResponse);

        _userRepository
            .Setup(x => x.GetUserByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

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

        _authService
            .Setup(x => x.LoginAsync(command.Email, command.Password))
            .ReturnsAsync(CreateTokenResponse());

        _userRepository
            .Setup(x => x.GetUserByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

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

        _authService
            .Setup(x => x.LoginAsync(command.Email, command.Password))
            .ReturnsAsync(CreateTokenResponse());

        _userRepository
            .Setup(x => x.GetUserByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

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

        _authService
            .Setup(x => x.LoginAsync(command.Email, command.Password))
            .ReturnsAsync(CreateTokenResponse());

        _userRepository
            .Setup(x => x.GetUserByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser());

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
        using var cts = new CancellationTokenSource();

        _authService
            .Setup(x => x.LoginAsync(command.Email, command.Password))
            .ReturnsAsync(CreateTokenResponse());

        _userRepository
            .Setup(x => x.GetUserByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser());

        // Act
        await sut.Handle(command, cts.Token);

        // Assert
        _userRepository.Verify(x => x.GetUserByEmailAsync(command.Email, cts.Token), Times.Once);
        _publishEndpoint.Verify(x => x.Publish(It.IsAny<UserLoginSagaStartedIntegrationEvent>(), cts.Token), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(cts.Token), Times.Once);
    }
}