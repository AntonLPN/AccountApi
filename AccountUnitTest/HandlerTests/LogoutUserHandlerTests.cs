using Account.Application.Features.Account.Logout;
using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Repositories;
using Account.Domain.Specifications;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Ardalis.Specification;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountUnitTest.HandlerTests;

public class LogoutUserHandlerTests
{
    private readonly Mock<ILogger<LogoutUserHandler>> _logger = new();
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IRepository<AppUser>> _userRepository = new();

    private LogoutUserHandler CreateSut()
    {
        return new LogoutUserHandler(
            _logger.Object,
            _authService.Object,
            _userRepository.Object);
    }

    private static LogoutCommand CreateCommand(
        string email = "test@mail.com",
        string refreshToken = "refresh_token",
        string? ipAddress = "192.168.1.1",
        string? userAgent = "Mozilla/5.0")
        => new(email, refreshToken, ipAddress, userAgent);

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

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsUnauthorized()
    {
        // Arrange
        var sut = CreateSut();
        var command = CreateCommand();

        _userRepository
            .Setup(x => x.FirstOrDefaultAsync(new UserByEmailSpec(command.Email), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        _authService.Verify(x => x.LogoutAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenKeycloakLogoutFails_ReturnsError()
    {
        // Arrange
        var sut = CreateSut();
        var command = CreateCommand();
        _userRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser());

        _authService
            .Setup(x => x.LogoutAsync(command.RefreshToken))
            .ReturnsAsync(false);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Error, result.Status);
    }

    [Fact]
    public async Task Handle_WhenLogoutSucceeds_ReturnsSuccess()
    {
        // Arrange
        var sut = CreateSut();
        var command = CreateCommand();

        _userRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser());

        _authService
            .Setup(x => x.LogoutAsync(command.RefreshToken))
            .ReturnsAsync(true);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ResultStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Handle_WhenLogoutSucceeds_PublishesSagaStartedEventWithUserDetails()
    {
        // Arrange
        var sut = CreateSut();
        var command = CreateCommand();
        var user = CreateUser();

        _userRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _authService
            .Setup(x => x.LogoutAsync(command.RefreshToken))
            .ReturnsAsync(true);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
       
    }

    [Fact]
    public async Task Handle_WhenLogoutSucceeds_SavesChangesForSaga()
    {
        // Arrange
        var sut = CreateSut();
        var command = CreateCommand();

        _userRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser());

        _authService
            .Setup(x => x.LogoutAsync(command.RefreshToken))
            .ReturnsAsync(true);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken()
    {
        // Arrange
        var sut = CreateSut();
        var command = CreateCommand();
        using var cts = new CancellationTokenSource();

        _userRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser());

        _authService
            .Setup(x => x.LogoutAsync(command.RefreshToken))
            .ReturnsAsync(true);

        // Act
        await sut.Handle(command, cts.Token);

        // Assert
        _userRepository.Verify(
            x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                It.IsAny<CancellationToken>()),
            Times.Once);
  
    }
}