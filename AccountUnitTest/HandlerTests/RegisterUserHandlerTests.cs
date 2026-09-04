using Account.Application.Features.Account.Register;
using Account.Domain.Entities;
using Account.Domain.Enums;
using Account.Domain.Interfaces;
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
    private readonly Mock<IUserAccountService> _userAccountService = new();

    private RegisterUserHandler CreateSut()
        => new(_logger.Object, _userRepository.Object, _coordinator.Object, _userAccountService.Object);

    private static RegisterCommand CreateCommand(
        AuthProvider provider = AuthProvider.LocalProvider,
        string email = "test@example.com",
        bool emailConfirmed = false,
        string password = "StrongP@ssw0rd!",
        string referrerCode = "REF123",
        string? ipAddress = "127.0.0.1",
        string? userAgent = "userAgent")
        => new(provider, email, emailConfirmed, password, referrerCode, ipAddress, userAgent);

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
    public async Task Handle_WhenUserAlreadyExists_DoesNotCallUserAccountService()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();

        SetupUserByEmail(new AppUser());

        await sut.Handle(cmd, CancellationToken.None);

        _userAccountService.Verify(
            x => x.RegisterUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyExists_DoesNotCallCoordinator()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();

        SetupUserByEmail(new AppUser());

        await sut.Handle(cmd, CancellationToken.None);

        _coordinator.Verify(
            x => x.RegisterAsync(It.IsAny<UserCoordinatorParams>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_CallsUserAccountServiceWithNormalizedEmail()
    {
        var sut = CreateSut();
        var cmd = CreateCommand(email: "Test@EXAMPLE.com");

        SetupUserByEmail(null);
        _userAccountService
            .Setup(x => x.RegisterUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(Result<string>.Success("user-id"));
        _coordinator
            .Setup(x => x.RegisterAsync(It.IsAny<UserCoordinatorParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RegisterUserResult>.Success(new RegisterUserResult()));

        await sut.Handle(cmd, CancellationToken.None);

        _userAccountService.Verify(
            x => x.RegisterUserAsync("test@example.com", cmd.Password, true),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserAccountServiceFails_ReturnsError()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var errorMessage = "Keycloak service unavailable";

        SetupUserByEmail(null);
        _userAccountService
            .Setup(x => x.RegisterUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(Result<string>.Error(errorMessage));

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(errorMessage, result.Errors);
    }

    [Fact]
    public async Task Handle_WhenUserAccountServiceFails_DoesNotCallCoordinator()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();

        SetupUserByEmail(null);
        _userAccountService
            .Setup(x => x.RegisterUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(Result<string>.Error("Registration failed"));

        await sut.Handle(cmd, CancellationToken.None);

        _coordinator.Verify(
            x => x.RegisterAsync(It.IsAny<UserCoordinatorParams>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserAccountServiceSucceeds_CallsCoordinatorWithCorrectParams()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var userId = "generated-user-id";

        SetupUserByEmail(null);
        _userAccountService
            .Setup(x => x.RegisterUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(Result<string>.Success(userId));
        _coordinator
            .Setup(x => x.RegisterAsync(It.IsAny<UserCoordinatorParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RegisterUserResult>.Success(new RegisterUserResult()));

        await sut.Handle(cmd, CancellationToken.None);

        _coordinator.Verify(
            x => x.RegisterAsync(
                It.Is<UserCoordinatorParams>(p => p.UserId == userId && p.RegisterCommand == cmd),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCoordinatorSucceeds_ReturnsCoordinatorResult()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var expectedResult = Result<RegisterUserResult>.Success(
            new RegisterUserResult { IsSuccess = true, Message = "Registration successful" });

        SetupUserByEmail(null);
        _userAccountService
            .Setup(x => x.RegisterUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(Result<string>.Success("user-id"));
        _coordinator
            .Setup(x => x.RegisterAsync(It.IsAny<UserCoordinatorParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.Same(expectedResult, result);
        Assert.True(result.IsSuccess);
        Assert.Equal("Registration successful", result.Value.Message);
    }

    [Fact]
    public async Task Handle_WhenCoordinatorFails_ReturnsCoordinatorError()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var errorResult = Result<RegisterUserResult>.Error("Coordinator failed");

        SetupUserByEmail(null);
        _userAccountService
            .Setup(x => x.RegisterUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(Result<string>.Success("user-id"));
        _coordinator
            .Setup(x => x.RegisterAsync(It.IsAny<UserCoordinatorParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorResult);

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Coordinator failed", result.Errors);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        using var cts = new CancellationTokenSource();

        SetupUserByEmail(null);
        _userAccountService
            .Setup(x => x.RegisterUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(Result<string>.Success("user-id"));
        _coordinator
            .Setup(x => x.RegisterAsync(It.IsAny<UserCoordinatorParams>(), cts.Token))
            .ReturnsAsync(Result<RegisterUserResult>.Success(new RegisterUserResult()));

        await sut.Handle(cmd, cts.Token);

        _userRepository.Verify(
            x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                cts.Token),
            Times.Once);
        _coordinator.Verify(
            x => x.RegisterAsync(It.IsAny<UserCoordinatorParams>(), cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_LogsRegisterAttempt()
    {
        var sut = CreateSut();
        var cmd = CreateCommand(email: "test@example.com");

        SetupUserByEmail(null);
        _userAccountService
            .Setup(x => x.RegisterUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(Result<string>.Success("user-id"));
        _coordinator
            .Setup(x => x.RegisterAsync(It.IsAny<UserCoordinatorParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RegisterUserResult>.Success(new RegisterUserResult()));

        await sut.Handle(cmd, CancellationToken.None);

        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Registering user")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithDifferentAuthProviders_Succeeds()
    {
        var sut = CreateSut();

        foreach (var provider in Enum.GetValues<AuthProvider>())
        {
            var cmd = CreateCommand(provider: provider);

            SetupUserByEmail(null);
            _userAccountService
                .Setup(x => x.RegisterUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(Result<string>.Success("user-id"));
            _coordinator
                .Setup(x => x.RegisterAsync(It.IsAny<UserCoordinatorParams>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<RegisterUserResult>.Success(new RegisterUserResult()));

            var result = await sut.Handle(cmd, CancellationToken.None);

            Assert.True(result.IsSuccess);
        }
    }

    [Fact]
    public async Task Handle_WithEmailConfirmedFlag_PassesToCoordinator()
    {
        var sut = CreateSut();
        var cmd = CreateCommand(emailConfirmed: true);

        SetupUserByEmail(null);
        _userAccountService
            .Setup(x => x.RegisterUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(Result<string>.Success("user-id"));
        _coordinator
            .Setup(x => x.RegisterAsync(It.IsAny<UserCoordinatorParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RegisterUserResult>.Success(new RegisterUserResult()));

        await sut.Handle(cmd, CancellationToken.None);

        _coordinator.Verify(
            x => x.RegisterAsync(
                It.Is<UserCoordinatorParams>(p => p.RegisterCommand.EmailConfirmed == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserAccountServiceReturnsErrorWithoutMessage_UsesDefaultMessage()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();

        SetupUserByEmail(null);
        _userAccountService
            .Setup(x => x.RegisterUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(Result<string>.Error());

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
        Assert.Equal("Registration failed", result.Errors.First());
    }
}
