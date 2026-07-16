using Account.Application.Features.Account.Register;
using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Domain.Repositories;
using Ardalis.Result;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountUnitTest.HandlerTests;

public class RegisterUserHandlerTests
{
    private readonly Mock<ILogger<RegisterUserHandler>> _logger = new();
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IApiKeyRepository> _apiKeyRepository = new();
    private readonly Mock<ICryptography> _cryptographyService = new();
    private readonly Mock<IAppDbTransaction> _tx = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly Mock<ILoginAuditRepository> _loginAuditRepository = new();
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
            _cryptographyService.Object,
            _publishEndpoint.Object,
            _loginAuditRepository.Object,
            _userAccountService.Object);
    }

    private static RegisterCommand CreateCommand(string email = "test@mail.com",
        string password = "123Avc_!@#$%^&*()_+")
        => new(email, password,"referrerId","127.0.0.1","userAgent");

    [Fact]
    public async Task Handle_WhenEmailExists_ReturnsConflict()
    {
        var sut = CreateSut();
        var command = CreateCommand();
        //Arrange
        _userRepository.Setup(x => x.GetUserByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppUser());
        //Act
        var result = await sut.Handle(command, CancellationToken.None);
        //Assets
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains("User already exists", result.Errors);

        _unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _loginAuditRepository.Verify(x => x.AddLogin(It.IsAny<LoginAudit>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAuthService_ReturnError()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        //Arrange
        _userRepository.Setup(x => x.GetUserByEmailAsync(cmd.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);
        _userAccountService.Setup(x => x.RegisterUserAsync(cmd.Email, cmd.Password, It.IsAny<bool>()))
            .ReturnsAsync(Result<string>.Error("Registration failed"));
        //Act
        var result = await sut.Handle(cmd, CancellationToken.None);
        //Assets
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Registration failed", result.Errors);

        _unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _loginAuditRepository.Verify(x => x.AddLogin(It.IsAny<LoginAudit>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAuthService_ReturnSuccess()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        //Arrange
        _userRepository.Setup(x => x.GetUserByEmailAsync(cmd.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);
        _userAccountService.Setup(x => x.RegisterUserAsync(cmd.Email, cmd.Password, It.IsAny<bool>()))
            .ReturnsAsync(Result<string>.Success("Registration Successful"));
        _userRepository.Setup(x => x.AddUser(It.IsAny<AppUser>()));
        _apiKeyRepository.Setup(x => x.CreateApiKey(It.IsAny<string>())).Returns("api_key");
        _cryptographyService.Setup(x => x.Hash(cmd.Password)).Returns("password_hash");
        _userRepository.Setup(x => x.FindByReferralCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);
        _authService.Setup(x => x.LoginAsync(cmd.Email, cmd.Password))
            .ReturnsAsync(new TokenResponse
            {
                AccessToken = "access_token",
                RefreshToken = "refresh_token",
                TokenType = "token_type",
                ExpiresIn = 3600,
                Scope = "scope"
            });
        _loginAuditRepository.Setup(x => x.AddLogin(It.IsAny<LoginAudit>(), It.IsAny<CancellationToken>()));
        
        //Act
        var result = await sut.Handle(cmd, CancellationToken.None);
        //Assets
        Assert.True(result.IsSuccess);
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal("api_key", result.Value.ApiKey);
        Assert.NotNull(result.Value.Token);
        Assert.Equal("access_token", result.Value.Token.AccessToken);
        Assert.Equal("refresh_token", result.Value.Token.RefreshToken);
        Assert.Equal("token_type", result.Value.Token.TokenType);
        Assert.Equal(3600, result.Value.Token.ExpiresIn);
        Assert.Equal("scope", result.Value.Token.Scope);
        //Db verify
        _apiKeyRepository.Verify(x => x.CreateApiKey(It.IsAny<string>()), Times.Once);
        _userRepository.Verify(x => x.AddUser(It.IsAny<AppUser>()), Times.Once);
        _unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _tx.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _tx.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        _loginAuditRepository.Verify(x => x.AddLogin(It.Is<LoginAudit>(a =>
            a.UserId == "Registration Successful" &&
            a.Email == cmd.Email &&
            a.IpAddress == cmd.IpAddress &&
            a.UserAgent == cmd.UserAgent), It.IsAny<CancellationToken>()), Times.Once);
    }
}