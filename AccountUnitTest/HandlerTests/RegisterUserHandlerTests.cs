using Account.Application.Features.Account.Register;
using Account.Domain.Entities;
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

public class RegisterUserHandlerTests
{
    private readonly Mock<ILogger<RegisterUserHandler>> _logger = new();
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRepository<AppUser>> _userRepository = new();
    private readonly Mock<IRepository<ApiKey>> _apiKeyRepository = new();
    private readonly Mock<IRepository<LoginAudit>> _loginAuditRepository = new();
    private readonly Mock<ICryptography> _cryptographyService = new();
    private readonly Mock<IAppDbTransaction> _tx = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly Mock<IUserAccountService> _userAccountService = new();

    private RegisterUserHandler CreateSut()
    {
        _unitOfWork
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tx.Object);

        return new RegisterUserHandler(
            _logger.Object,
            _authService.Object,
            _unitOfWork.Object,
            _userRepository.Object,
            _apiKeyRepository.Object,
            _loginAuditRepository.Object,
            _cryptographyService.Object,
            _publishEndpoint.Object,
            _userAccountService.Object);
    }

    private static RegisterCommand CreateCommand(string email = "test@mail.com",
        string password = "123Avc_!@#$%^&*()_+")
        => new(email, password, "referrerId", "127.0.0.1", "userAgent");

    [Fact]
    public async Task Handle_WhenEmailExists_ReturnsConflict()
    {
        var sut = CreateSut();
        var command = CreateCommand();

        //Arrange
        _userRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppUser());

        //Act
        var result = await sut.Handle(command, CancellationToken.None);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains("User already exists", result.Errors);

        _unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _loginAuditRepository.Verify(x => x.AddAsync(It.IsAny<LoginAudit>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _userRepository.Verify(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByReferralCodeSpec),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAuthService_ReturnError()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();

        //Arrange
        _userRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);

        _userAccountService
            .Setup(x => x.RegisterUserAsync(cmd.Email, cmd.Password, true))
            .ReturnsAsync(Result<string>.Error("Registration failed"));

        //Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Registration failed", result.Errors);

        _unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _loginAuditRepository.Verify(x => x.AddAsync(It.IsAny<LoginAudit>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAuthService_ReturnSuccess()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        const string keycloakUserId = "keycloak-user-id";

        //Arrange
        _userRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);

        _userRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByReferralCodeSpec),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);

        _userAccountService
            .Setup(x => x.RegisterUserAsync(cmd.Email, cmd.Password, true))
            .ReturnsAsync(Result<string>.Success(keycloakUserId));

        _cryptographyService
            .Setup(x => x.Hash(cmd.Password))
            .Returns("password_hash");

        _userRepository
            .Setup(x => x.AddAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser u, CancellationToken _) => u);

        _apiKeyRepository
            .Setup(x => x.AddAsync(It.IsAny<ApiKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiKey k, CancellationToken _) => k);

        _loginAuditRepository
            .Setup(x => x.AddAsync(It.IsAny<LoginAudit>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoginAudit l, CancellationToken _) => l);

        _publishEndpoint
            .Setup(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _authService
            .Setup(x => x.LoginAsync(cmd.Email, cmd.Password))
            .ReturnsAsync(new TokenResponse
            {
                AccessToken = "access_token",
                RefreshToken = "refresh_token",
                TokenType = "token_type",
                ExpiresIn = 3600,
                Scope = "scope"
            });

        //Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Value);
        Assert.NotNull(result.Value.ApiKeys);
        Assert.NotNull(result.Value.Token);
        Assert.Equal("access_token", result.Value.Token.AccessToken);
        Assert.Equal("refresh_token", result.Value.Token.RefreshToken);
        Assert.Equal("token_type", result.Value.Token.TokenType);
        Assert.Equal(3600, result.Value.Token.ExpiresIn);
        Assert.Equal("scope", result.Value.Token.Scope);

        //Db verify
        _userRepository.Verify(x => x.AddAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()), Times.Once);
        _apiKeyRepository.Verify(x => x.AddAsync(It.IsAny<ApiKey>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _tx.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _tx.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);

        _loginAuditRepository.Verify(x => x.AddAsync(It.Is<LoginAudit>(a =>
            a.UserId == keycloakUserId &&
            a.Email == cmd.Email &&
            a.IpAddress == cmd.IpAddress &&
            a.UserAgent == cmd.UserAgent), It.IsAny<CancellationToken>()), Times.Once);
    }
}