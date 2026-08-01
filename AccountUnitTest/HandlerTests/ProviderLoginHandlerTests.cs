using Account.Application.Features.Account.ProviderLogin;
using Account.Application.Interfaces;
using Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;
using Account.Domain.Entities;
using Account.Domain.Enums;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Domain.Repositories;
using Account.Domain.Specifications;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Ardalis.Specification;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountUnitTest.HandlerTests;

public class ProviderLoginHandlerTests
{
    private readonly Mock<ILogger<ProviderLoginHandler>> _logger = new();
    private readonly Mock<IProviderValidator> _providerValidator = new();
    private readonly Mock<IRepository<AppUser>> _userRepository = new();
    private readonly Mock<IApiKeyRepository> _apiKeyRepository = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAuthService> _authService = new();

    private ProviderLoginHandler CreateSut()
        => new(
            _logger.Object,
            _providerValidator.Object,
            _userRepository.Object,
            _apiKeyRepository.Object,
            _publishEndpoint.Object,
            _unitOfWork.Object,
            _authService.Object);

    private static ProviderLoginCommand CreateCommand(string token = "google_token",
        string? ipAddress = "127.0.0.1", string? userAgent = "userAgent")
        => new(token, AuthProviders.Google, ipAddress, userAgent);

    private void SetupProviderValidate(string? email = "test@gmail.com")
        => _providerValidator
            .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(It.IsAny<AuthProviders>(), It.IsAny<string>()))
            .ReturnsAsync(email);

    private void SetupUserByEmail(AppUser? user)
        => _userRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

    private static TokenResponse CreateTokenResponse() => new()
    {
        AccessToken = "access_token",
        RefreshToken = "refresh_token",
        TokenType = "Bearer",
        ExpiresIn = 3600,
        Scope = "openid"
    };

    [Fact]
    public async Task Handle_WhenEmailFromProviderIsNull_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();

        SetupProviderValidate(null);

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.Handle(cmd, CancellationToken.None));

        _userRepository.Verify(x => x.FirstOrDefaultAsync(
            It.IsAny<ISpecification<AppUser>>(), It.IsAny<CancellationToken>()), Times.Never);
        _authService.Verify(x => x.LoginAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmailFromProviderIsEmpty_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();

        SetupProviderValidate(string.Empty);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.Handle(cmd, CancellationToken.None));

        _userRepository.Verify(x => x.FirstOrDefaultAsync(
            It.IsAny<ISpecification<AppUser>>(), It.IsAny<CancellationToken>()), Times.Never);
        _authService.Verify(x => x.LoginAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsUnauthorized()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        const string email = "test@gmail.com";

        SetupProviderValidate(email);
        SetupUserByEmail(null);

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Unauthorized, result.Status);

        _apiKeyRepository.Verify(x => x.GetApiKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _authService.Verify(x => x.LoginAsync(It.IsAny<string>()), Times.Never);
        _publishEndpoint.Verify(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAuthServiceReturnsNull_ThrowsAndLogsError()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        const string email = "test@gmail.com";
        var user = new AppUser { Id = "user-id", Email = email };

        SetupProviderValidate(email);
        SetupUserByEmail(user);
        _apiKeyRepository
            .Setup(x => x.GetApiKeyAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync("api-key");
        _authService
            .Setup(x => x.LoginAsync(email))
            .ReturnsAsync((TokenResponse?)null);

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.Handle(cmd, CancellationToken.None));

        _publishEndpoint.Verify(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        _logger.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<ArgumentNullException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSuccess_ReturnsApiKeyAndToken()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        const string email = "test@gmail.com";
        const string apiKey = "api-key-abc";
        var user = new AppUser { Id = "user-id-123", Email = email };
        var token = CreateTokenResponse();

        SetupProviderValidate(email);
        SetupUserByEmail(user);
        _apiKeyRepository
            .Setup(x => x.GetApiKeyAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiKey);
        _authService
            .Setup(x => x.LoginAsync(email))
            .ReturnsAsync(token);
        _publishEndpoint
            .Setup(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(apiKey, result.Value.ApiKey);
        Assert.Equal(token.AccessToken, result.Value.Token!.AccessToken);
        Assert.Equal(token.RefreshToken, result.Value.Token.RefreshToken);

        _publishEndpoint.Verify(x => x.Publish(
            It.Is<UserLoginSagaStartedIntegrationEvent>(e =>
                e.UserId == user.Id &&
                e.Email == user.Email &&
                e.IpAddress == cmd.IpAddress &&
                e.UserAgent == cmd.UserAgent),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenApiKeyIsNull_ReturnsSuccessWithEmptyApiKey()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        const string email = "test@gmail.com";
        var user = new AppUser { Id = "user-id-123", Email = email };

        SetupProviderValidate(email);
        SetupUserByEmail(user);
        _apiKeyRepository
            .Setup(x => x.GetApiKeyAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        _authService
            .Setup(x => x.LoginAsync(email))
            .ReturnsAsync(CreateTokenResponse());
        _publishEndpoint
            .Setup(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Value.ApiKey);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        const string email = "test@gmail.com";
        var user = new AppUser { Id = "user-id-123", Email = email };
        using var cts = new CancellationTokenSource();

        SetupProviderValidate(email);
        SetupUserByEmail(user);
        _apiKeyRepository
            .Setup(x => x.GetApiKeyAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync("api-key");
        _authService
            .Setup(x => x.LoginAsync(email))
            .ReturnsAsync(CreateTokenResponse());
        _publishEndpoint
            .Setup(x => x.Publish(It.IsAny<UserLoginSagaStartedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await sut.Handle(cmd, cts.Token);

        _userRepository.Verify(x => x.FirstOrDefaultAsync(
            It.IsAny<ISpecification<AppUser>>(), It.IsAny<CancellationToken>()), Times.Once);
        _apiKeyRepository.Verify(x => x.GetApiKeyAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        _publishEndpoint.Verify(x => x.Publish(It.IsAny<UserLoginSagaStartedIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}