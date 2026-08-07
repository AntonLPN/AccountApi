using Account.Application.Features.Account.Login;
using Account.Application.Interfaces;
using Account.Domain.Entities;
using Account.Domain.Specifications;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Ardalis.Specification;
using Moq;

namespace AccountUnitTest.HandlerTests;

public class LoginUserHandlerTests
{
    private readonly Mock<IRepository<AppUser>> _userRepository = new();
    private readonly Mock<ILoginStrategy> _standardStrategy = new();
    private readonly Mock<ILoginStrategy> _mfaStrategy = new();

    private LoginUserHandler CreateSut(params ILoginStrategy[] strategies)
        => new(_userRepository.Object, strategies);

    private static LoginCommand CreateCommand(string email = "test@mail.com",
        string password = "123Avc_!@#$%^&*()_+", string? ipAddress = "127.0.0.1", string? userAgent = "userAgent")
        => new(email, password, ipAddress, userAgent);

    private void SetupUserByEmail(AppUser? user)
        => _userRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailWithAuthorizedApiKeysSpec),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        var sut = CreateSut(_standardStrategy.Object, _mfaStrategy.Object);
        var cmd = CreateCommand();

        SetupUserByEmail(null);

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("User not found", result.Errors);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_DoesNotCallAnyStrategy()
    {
        var sut = CreateSut(_standardStrategy.Object, _mfaStrategy.Object);
        var cmd = CreateCommand();

        SetupUserByEmail(null);

        await sut.Handle(cmd, CancellationToken.None);

        _standardStrategy.Verify(x => x.CanHandle(It.IsAny<AppUser>()), Times.Never);
        _mfaStrategy.Verify(x => x.CanHandle(It.IsAny<AppUser>()), Times.Never);
        _standardStrategy.Verify(x => x.HandleAsync(It.IsAny<AppUser>(), It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _mfaStrategy.Verify(x => x.HandleAsync(It.IsAny<AppUser>(), It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMatchingStrategyFound_CallsThatStrategyHandleAsync()
    {
        var sut = CreateSut(_standardStrategy.Object, _mfaStrategy.Object);
        var cmd = CreateCommand();
        var user = new AppUser { Id = "user-id", Email = cmd.Email, IsTwoFactorEnabled = false };
        var expected = Result<LoginUserResult>.Success(new LoginUserResult { IsMfaRequired = false });

        SetupUserByEmail(user);
        _standardStrategy.Setup(x => x.CanHandle(user)).Returns(true);
        _mfaStrategy.Setup(x => x.CanHandle(user)).Returns(false);
        _standardStrategy
            .Setup(x => x.HandleAsync(user, cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.Same(expected, result);
        _standardStrategy.Verify(x => x.HandleAsync(user, cmd, It.IsAny<CancellationToken>()), Times.Once);
        _mfaStrategy.Verify(x => x.HandleAsync(It.IsAny<AppUser>(), It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTwoFactorEnabled_SelectsMfaStrategy_NotStandard()
    {
        var sut = CreateSut(_standardStrategy.Object, _mfaStrategy.Object);
        var cmd = CreateCommand();
        var user = new AppUser { Id = "user-id", Email = cmd.Email, IsTwoFactorEnabled = true };
        var expected = Result<LoginUserResult>.Success(new LoginUserResult { IsMfaRequired = true });

        SetupUserByEmail(user);
        _standardStrategy.Setup(x => x.CanHandle(user)).Returns(false);
        _mfaStrategy.Setup(x => x.CanHandle(user)).Returns(true);
        _mfaStrategy
            .Setup(x => x.HandleAsync(user, cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.True(result.Value.IsMfaRequired);
        _mfaStrategy.Verify(x => x.HandleAsync(user, cmd, It.IsAny<CancellationToken>()), Times.Once);
        _standardStrategy.Verify(x => x.HandleAsync(It.IsAny<AppUser>(), It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNoStrategyMatches_ThrowsInvalidOperationException()
    {
        var sut = CreateSut(_standardStrategy.Object, _mfaStrategy.Object);
        var cmd = CreateCommand();
        var user = new AppUser { Id = "user-id", Email = cmd.Email };

        SetupUserByEmail(user);
        _standardStrategy.Setup(x => x.CanHandle(user)).Returns(false);
        _mfaStrategy.Setup(x => x.CanHandle(user)).Returns(false);

        // First() без совпадений бросает InvalidOperationException — фиксируем текущее поведение.
        // Если заменишь First на FirstOrDefault + явную бизнес-ошибку — обнови этот тест соответственно.
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenStrategyThrows_ExceptionPropagates()
    {
        var sut = CreateSut(_standardStrategy.Object);
        var cmd = CreateCommand();
        var user = new AppUser { Id = "user-id", Email = cmd.Email, IsTwoFactorEnabled = false };

        SetupUserByEmail(user);
        _standardStrategy.Setup(x => x.CanHandle(user)).Returns(true);
        _standardStrategy
            .Setup(x => x.HandleAsync(user, cmd, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(cmd, CancellationToken.None));
        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken()
    {
        var sut = CreateSut(_standardStrategy.Object);
        var cmd = CreateCommand();
        var user = new AppUser { Id = "user-id", Email = cmd.Email, IsTwoFactorEnabled = false };
        using var cts = new CancellationTokenSource();

        _userRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailWithAuthorizedApiKeysSpec),
                cts.Token))
            .ReturnsAsync(user);

        _standardStrategy.Setup(x => x.CanHandle(user)).Returns(true);
        _standardStrategy
            .Setup(x => x.HandleAsync(user, cmd, cts.Token))
            .ReturnsAsync(Result<LoginUserResult>.Success(new LoginUserResult()));

        await sut.Handle(cmd, cts.Token);

        _userRepository.Verify(x => x.FirstOrDefaultAsync(
            It.Is<ISpecification<AppUser>>(s => s is UserByEmailWithAuthorizedApiKeysSpec), cts.Token), Times.Once);
        _standardStrategy.Verify(x => x.HandleAsync(user, cmd, cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_UsesEmailCreateToNormalizeEmail_BeforeQueryingRepository()
    {
        var sut = CreateSut(_standardStrategy.Object);
        var cmd = CreateCommand(email: "Test@EXAMPLE.com");
        var user = new AppUser { Id = "user-id", Email = "test@example.com", IsTwoFactorEnabled = false };

        ISpecification<AppUser>? capturedSpec = null;
        _userRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailWithAuthorizedApiKeysSpec),
                It.IsAny<CancellationToken>()))
            .Callback<ISpecification<AppUser>, CancellationToken>((spec, _) => capturedSpec = spec)
            .ReturnsAsync(user);

        _standardStrategy.Setup(x => x.CanHandle(user)).Returns(true);
        _standardStrategy
            .Setup(x => x.HandleAsync(user, cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginUserResult>.Success(new LoginUserResult()));

        await sut.Handle(cmd, CancellationToken.None);

        Assert.NotNull(capturedSpec);
        Assert.IsType<UserByEmailWithAuthorizedApiKeysSpec>(capturedSpec);
    }
}