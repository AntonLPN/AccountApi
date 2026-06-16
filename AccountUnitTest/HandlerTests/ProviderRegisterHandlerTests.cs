using Account.Application.Features.Account.ProvidersRegister;
using Account.Contracts.SagaEvents.UserRegisterSagaEvents.Events;
using Account.Domain.DTOs;
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

public class ProviderRegisterHandlerTests
{
    private readonly Mock<ILogger<ProviderRegisterHandler>> _logger = new();
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IApiKeyRepository> _apiKeyRepository = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly Mock<IAppDbTransaction> _tx = new();

    private ProviderRegisterHandler CreateSut()
    {
        _unitOfWork
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tx.Object);

        return new ProviderRegisterHandler(
            _logger.Object,
            _userRepository.Object,
            _authService.Object,
            _unitOfWork.Object,
            _apiKeyRepository.Object,
            _publishEndpoint.Object);
    }

    private static ProviderRegisterCommand CreateCommand(string token = "google_token", string referrerCode = "REF123")
        => new(token, referrerCode,AuthProviders.Google);

    private void SetupGoogleValidate(string email = "test@gmail.com")
        => _authService
            .Setup(x => x.GoogleValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(new GooglePayloadDto { Email = email });

    [Fact]
    public async Task Handle_WhenUserAlreadyExists_ReturnsConflict()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        SetupGoogleValidate();

        _userRepository
            .Setup(x => x.GetUserByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppUser());

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains("User already exists", result.Errors);

        _unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _authService.Verify(x => x.RegisterUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSuccess_ReturnsTokenAndApiKey()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        const string email = "test@gmail.com";
        const string userId = "user-id-123";
        const string apiKey = "api-key-abc";
        var token = new TokenResponse
        {
            AccessToken = "access_token",
            RefreshToken = "refresh_token",
            TokenType = "Bearer",
            ExpiresIn = 3600,
            Scope = "openid"
        };

        SetupGoogleValidate(email);
        _userRepository
            .Setup(x => x.GetUserByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);
        _authService
            .Setup(x => x.RegisterUserAsync(email, "", false))
            .ReturnsAsync(Result<string>.Success(userId));
        _authService
            .Setup(x => x.LoginByEmailWithoutPasswordAsync(email))
            .ReturnsAsync(token);
        _userRepository
            .Setup(x => x.FindByReferralCodeAsync(cmd.ReferrerCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);
        _apiKeyRepository
            .Setup(x => x.CreateApiKey(It.IsAny<string>()))
            .Returns(apiKey);

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(apiKey, result.Value.ApiKey);
        Assert.Equal("access_token", result.Value.Token!.AccessToken);
        Assert.Equal("refresh_token", result.Value.Token.RefreshToken);

        _userRepository.Verify(x => x.AddUser(It.IsAny<AppUser>()), Times.Once);
        _apiKeyRepository.Verify(x => x.CreateApiKey(It.IsAny<string>()), Times.Once);
        _publishEndpoint.Verify(x => x.Publish(It.IsAny<UserSagaStartedIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _tx.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRegisterFails_ThrowsAndCallsCleanup()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        const string email = "test@gmail.com";

        SetupGoogleValidate(email);
        _userRepository
            .Setup(x => x.GetUserByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);
        _authService
            .Setup(x => x.RegisterUserAsync(email, "", false))
            .ThrowsAsync(new Exception("Keycloak unavailable"));
        _authService
            .Setup(x => x.DeleteUserByEmailAsync(email))
            .ReturnsAsync(Result.Success());

        await Assert.ThrowsAsync<Exception>(() => sut.Handle(cmd, CancellationToken.None));

        _authService.Verify(x => x.DeleteUserByEmailAsync(email), Times.Once);
        _unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenReferrerCodeMatches_SetsReferrerId()
    {
        var sut = CreateSut();
        var cmd = CreateCommand(referrerCode: "VALID_REF");
        const string email = "test@gmail.com";
        var referrer = AppUser.Create(new AppUserCreateParams("ref-user-id", "referrer@mail.com", null, null));

        SetupGoogleValidate(email);
        _userRepository
            .Setup(x => x.GetUserByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);
        _authService
            .Setup(x => x.RegisterUserAsync(email, "", false))
            .ReturnsAsync(Result<string>.Success("new-user-id"));
        _authService
            .Setup(x => x.LoginByEmailWithoutPasswordAsync(email))
            .ReturnsAsync(new TokenResponse { AccessToken = "token" });
        _userRepository
            .Setup(x => x.FindByReferralCodeAsync("VALID_REF", It.IsAny<CancellationToken>()))
            .ReturnsAsync(referrer);
        _apiKeyRepository
            .Setup(x => x.CreateApiKey(It.IsAny<string>()))
            .Returns("api-key");

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _userRepository.Verify(x => x.FindByReferralCodeAsync("VALID_REF", It.IsAny<CancellationToken>()), Times.Once);
        _userRepository.Verify(x => x.AddUser(It.Is<AppUser>(u => u.ReferrerId == referrer.Id)), Times.Once);
    }
}
