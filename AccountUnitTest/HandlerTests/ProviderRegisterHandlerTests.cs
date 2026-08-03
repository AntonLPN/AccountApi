using Account.Application.Features.Account.ProvidersRegister;
using Account.Application.Interfaces;
using Account.Contracts.Saga.UserRegisterSagaEvents.Events;
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

public class ProviderRegisterHandlerTests
{
    private readonly Mock<ILogger<ProviderRegisterHandler>> _logger = new();
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRepository<AppUser>> _userRepository = new();
    private readonly Mock<IRepository<ApiKey>> _apiKeyRepository = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly Mock<IAppDbTransaction> _tx = new();
    private readonly Mock<IProviderValidator> _providerValidator = new();
    private readonly Mock<IRepository<LoginAudit>> _loginAuditRepository = new();
    private readonly Mock<IUserAccountService> _userAccountService = new();

    private ProviderRegisterHandler CreateSut()
    {
        _unitOfWork
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tx.Object);

        _userRepository
            .Setup(x => x.AddAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser u, CancellationToken _) => u);

        _apiKeyRepository
            .Setup(x => x.AddAsync(It.IsAny<ApiKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiKey k, CancellationToken _) => k);

        _loginAuditRepository
            .Setup(x => x.AddAsync(It.IsAny<LoginAudit>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoginAudit a, CancellationToken _) => a);

        return new ProviderRegisterHandler(
            _logger.Object,
            _userRepository.Object,
            _authService.Object,
            _unitOfWork.Object,
            _apiKeyRepository.Object,
            _publishEndpoint.Object,
            _providerValidator.Object,
            _loginAuditRepository.Object,
            _userAccountService.Object);
    }

    private static ProviderRegisterCommand CreateCommand(string token = "google_token", string referrerCode = "REF123")
        => new(token, referrerCode, AuthProviders.Google, "127.0.0.1", "userAgent");

    private void SetupProviderValidate(string email = "test@gmail.com")
        => _providerValidator
            .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(It.IsAny<AuthProviders>(), It.IsAny<string>()))
            .ReturnsAsync(email);

    private void SetupUserByEmail(AppUser? user)
        => _userRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

    private void SetupUserByReferralCode(AppUser? user)
        => _userRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByReferralCodeSpec),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

    [Fact]
    public async Task Handle_WhenUserAlreadyExists_ReturnsConflict()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        SetupProviderValidate();

        SetupUserByEmail(new AppUser());

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains("User already exists", result.Errors);

        _unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _userAccountService.Verify(x => x.RegisterUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        _loginAuditRepository.Verify(x => x.AddAsync(It.IsAny<LoginAudit>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSuccess_ReturnsTokenAndApiKey()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        const string email = "test@gmail.com";
        const string userId = "user-id-123";
        var token = new TokenResponse
        {
            AccessToken = "access_token",
            RefreshToken = "refresh_token",
            TokenType = "Bearer",
            ExpiresIn = 3600,
            Scope = "openid"
        };

        SetupProviderValidate(email);
        SetupUserByEmail(null);
        _userAccountService
            .Setup(x => x.RegisterUserAsync(email, "", false))
            .ReturnsAsync(Result<string>.Success(userId));
        _authService
            .Setup(x => x.LoginAsync(email))
            .ReturnsAsync(token);
        SetupUserByReferralCode(null);
        _loginAuditRepository.Setup(x => x.AddAsync(It.IsAny<LoginAudit>(), It.IsAny<CancellationToken>()));

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("access_token", result.Value.Token!.AccessToken);
        Assert.Equal("refresh_token", result.Value.Token.RefreshToken);

        _userRepository.Verify(x => x.AddAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()), Times.Once);
        _apiKeyRepository.Verify(x => x.AddAsync(It.IsAny<ApiKey>(), It.IsAny<CancellationToken>()), Times.Once);
        _publishEndpoint.Verify(x => x.Publish(It.IsAny<UserRegisterSagaStartedIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _tx.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _loginAuditRepository.Verify(x => x.AddAsync(It.Is<LoginAudit>(a =>
            a.UserId == userId &&
            a.Email == email &&
            a.IpAddress == cmd.IpAddress &&
            a.UserAgent == cmd.UserAgent), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRegisterFails_ThrowsAndCallsCleanup()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        const string email = "test@gmail.com";

        SetupProviderValidate(email);
        SetupUserByEmail(null);
        _userAccountService
            .Setup(x => x.RegisterUserAsync(email, "", false))
            .ThrowsAsync(new Exception("Keycloak unavailable"));
        _userAccountService
            .Setup(x => x.DeleteUserAsync(email))
            .ReturnsAsync(Result.Success());

        await Assert.ThrowsAsync<Exception>(() => sut.Handle(cmd, CancellationToken.None));

        _userAccountService.Verify(x => x.DeleteUserAsync(email), Times.Once);
        _unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _loginAuditRepository.Verify(x => x.AddAsync(It.IsAny<LoginAudit>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenReferrerCodeMatches_SetsReferrerId()
    {
        var sut = CreateSut();
        var cmd = CreateCommand(referrerCode: "VALID_REF");
        const string email = "test@gmail.com";
        var referrer = AppUser.Create(new AppUserCreateParams("ref-user-id", "referrer@mail.com", null, null));

        SetupProviderValidate(email);
        SetupUserByEmail(null);
        _userAccountService
            .Setup(x => x.RegisterUserAsync(email, "", false))
            .ReturnsAsync(Result<string>.Success("new-user-id"));
        _authService
            .Setup(x => x.LoginAsync(email))
            .ReturnsAsync(new TokenResponse { AccessToken = "token" });
        SetupUserByReferralCode(referrer);
        _loginAuditRepository.Setup(x => x.AddAsync(It.IsAny<LoginAudit>(), It.IsAny<CancellationToken>()));

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _userRepository.Verify(x => x.FirstOrDefaultAsync(
            It.Is<ISpecification<AppUser>>(s => s is UserByReferralCodeSpec),
            It.IsAny<CancellationToken>()), Times.Once);
        _userRepository.Verify(x => x.AddAsync(
            It.Is<AppUser>(u => u.ReferrerId == referrer.Id), It.IsAny<CancellationToken>()), Times.Once);
        _loginAuditRepository.Verify(x => x.AddAsync(It.Is<LoginAudit>(a =>
            a.UserId == "new-user-id" &&
            a.Email == email &&
            a.IpAddress == cmd.IpAddress &&
            a.UserAgent == cmd.UserAgent), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSuccess_PublishesSagaStartedEvent()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        const string email = "test@gmail.com";
        const string userId = "user-id-123";

        SetupProviderValidate(email);
        SetupUserByEmail(null);
        _userAccountService
            .Setup(x => x.RegisterUserAsync(email, "", false))
            .ReturnsAsync(Result<string>.Success(userId));
        _authService
            .Setup(x => x.LoginAsync(email))
            .ReturnsAsync(new TokenResponse { AccessToken = "token" });
        SetupUserByReferralCode(null);
        _loginAuditRepository.Setup(x => x.AddAsync(It.IsAny<LoginAudit>(), It.IsAny<CancellationToken>()));

        await sut.Handle(cmd, CancellationToken.None);

        _publishEndpoint.Verify(x => x.Publish(
            It.Is<UserRegisterSagaStartedIntegrationEvent>(e =>
                e.CorrelationId != Guid.Empty &&
                e.Email == email &&
                e.UserId == userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSuccess_BeginTransactionAndCommit()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        const string email = "test@gmail.com";

        SetupProviderValidate(email);
        SetupUserByEmail(null);
        _userAccountService
            .Setup(x => x.RegisterUserAsync(email, "", false))
            .ReturnsAsync(Result<string>.Success("user-id"));
        _authService
            .Setup(x => x.LoginAsync(email))
            .ReturnsAsync(new TokenResponse { AccessToken = "token" });
        SetupUserByReferralCode(null);
        _loginAuditRepository.Setup(x => x.AddAsync(It.IsAny<LoginAudit>(), It.IsAny<CancellationToken>()));

        await sut.Handle(cmd, CancellationToken.None);

        _unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _tx.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        const string email = "test@gmail.com";
        using var cts = new CancellationTokenSource();

        SetupProviderValidate(email);
        SetupUserByEmail(null);
        _userAccountService
            .Setup(x => x.RegisterUserAsync(email, "", false))
            .ReturnsAsync(Result<string>.Success("user-id"));
        _authService
            .Setup(x => x.LoginAsync(email))
            .ReturnsAsync(new TokenResponse { AccessToken = "token" });
        SetupUserByReferralCode(null);
        _loginAuditRepository.Setup(x => x.AddAsync(It.IsAny<LoginAudit>(), It.IsAny<CancellationToken>()));

        await sut.Handle(cmd, cts.Token);

        _userRepository.Verify(x => x.FirstOrDefaultAsync(
            It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec), cts.Token), Times.Once);
        _userRepository.Verify(x => x.FirstOrDefaultAsync(
            It.Is<ISpecification<AppUser>>(s => s is UserByReferralCodeSpec), cts.Token), Times.Once);
        _userRepository.Verify(x => x.AddAsync(It.IsAny<AppUser>(), cts.Token), Times.Once);
        _apiKeyRepository.Verify(x => x.AddAsync(It.IsAny<ApiKey>(), cts.Token), Times.Once);
        _loginAuditRepository.Verify(x => x.AddAsync(It.IsAny<LoginAudit>(), cts.Token), Times.Once);
        _publishEndpoint.Verify(x => x.Publish(It.IsAny<UserRegisterSagaStartedIntegrationEvent>(), cts.Token), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(cts.Token), Times.Once);
        _unitOfWork.Verify(x => x.BeginTransactionAsync(cts.Token), Times.Once);
        _tx.Verify(x => x.CommitAsync(cts.Token), Times.Once);
    }
}