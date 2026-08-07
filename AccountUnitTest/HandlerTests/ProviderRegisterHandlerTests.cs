using Account.Application.Features.Account.Register;
using Account.Domain.Entities;
using Account.Domain.Models;
using Account.Domain.Specifications;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Ardalis.Specification;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountUnitTest.HandlerTests;

public class RegisterUserHandlerTests
{
    private readonly Mock<ILogger<RegisterUserHandler>> _logger = new();
    private readonly Mock<IRepository<AppUser>> _userRepository = new();
    private readonly Mock<IUserRegistrationCoordinator> _coordinator = new();

    private RegisterUserHandler CreateSut()
        => new(_logger.Object, _userRepository.Object, _coordinator.Object);

    private static RegisterCommand CreateCommand(string email = "test@example.com", string referrerCode = "REF123")
        => new(email, "StrongP@ssw0rd!", referrerCode, "127.0.0.1", "userAgent");

    private void SetupUserByEmail(AppUser? user)
        => _userRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

    [Fact]
    public async Task Handle_WhenUserAlreadyExists_ReturnsConflict()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();

        SetupUserByEmail(new AppUser());

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains("User already exists", result.Errors);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyExists_DoesNotCallCoordinator()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();

        SetupUserByEmail(new AppUser());

        await sut.Handle(cmd, CancellationToken.None);

        _coordinator.Verify(x => x.RegisterAsync(It.IsAny<RegisterCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_CallsCoordinatorWithSameCommand()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();

        SetupUserByEmail(null);
        _coordinator
            .Setup(x => x.RegisterAsync(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RegisterUserResult>.Success(new RegisterUserResult()));

        await sut.Handle(cmd, CancellationToken.None);

        _coordinator.Verify(x => x.RegisterAsync(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ReturnsCoordinatorSuccessResult()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var expected = Result<RegisterUserResult>.Success(new RegisterUserResult
        {
            ApiKeys = ["test-api-key"],
            Token = new TokenResponse { AccessToken = "access-token", TokenType = "Bearer" }
        });

        SetupUserByEmail(null);
        _coordinator
            .Setup(x => x.RegisterAsync(It.IsAny<RegisterCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Value.Token!.AccessToken);
        Assert.Equal(expected.Value.ApiKeys, result.Value.ApiKeys);
    }

    [Fact]
    public async Task Handle_WhenCoordinatorReturnsError_PropagatesError()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var errorResult = Result<RegisterUserResult>.Error("Registration succeeded, but automatic login failed.");

        SetupUserByEmail(null);
        _coordinator
            .Setup(x => x.RegisterAsync(It.IsAny<RegisterCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorResult);

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Registration succeeded, but automatic login failed.", result.Errors);
    }

    [Fact]
    public async Task Handle_WhenCoordinatorThrows_ExceptionPropagates()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();

        SetupUserByEmail(null);
        _coordinator
            .Setup(x => x.RegisterAsync(It.IsAny<RegisterCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(cmd, CancellationToken.None));
        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        using var cts = new CancellationTokenSource();

        SetupUserByEmail(null);
        _coordinator
            .Setup(x => x.RegisterAsync(It.IsAny<RegisterCommand>(), cts.Token))
            .ReturnsAsync(Result<RegisterUserResult>.Success(new RegisterUserResult()));

        await sut.Handle(cmd, cts.Token);

        _userRepository.Verify(x => x.FirstOrDefaultAsync(
            It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec), cts.Token), Times.Once);
        _coordinator.Verify(x => x.RegisterAsync(cmd, cts.Token), Times.Once);
    }
}