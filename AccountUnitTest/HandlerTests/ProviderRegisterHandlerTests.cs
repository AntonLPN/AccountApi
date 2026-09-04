using Account.Application.Features.Account.ProvidersRegister;
using Account.Application.Interfaces;
using Account.Domain.Entities;
using Account.Domain.Enums;
using Account.Domain.Specifications;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Ardalis.Specification;
using Moq;

namespace AccountUnitTest.HandlerTests;

public class ProviderRegisterHandlerTests
{
    private readonly Mock<IProviderValidator> _providerValidator = new();
    private readonly Mock<IRepository<AppUser>> _userRepository = new();
    private readonly Mock<IProviderRegistrationCoordinator> _coordinator = new();

    private ProviderRegisterHandler CreateSut()
        => new(_providerValidator.Object, _userRepository.Object, _coordinator.Object);

    private static ProviderRegisterCommand CreateCommand(
        string providerToken = "valid-token",
        string referrerCode = "REF123",
        AuthProvider provider = AuthProvider.Google,
        string? ipAddress = "127.0.0.1",
        string? userAgent = "userAgent")
        => new(providerToken, referrerCode, provider, ipAddress, userAgent);

    private void SetupUserByEmail(AppUser? user)
        => _userRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

    [Fact]
    public async Task Handle_WhenProviderTokenIsInvalid_ReturnsError()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();

        _providerValidator
            .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(cmd.Provider, cmd.ProviderToken))
            .ReturnsAsync((string?)null);

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Invalid provider token", result.Errors);
    }

    [Fact]
    public async Task Handle_WhenProviderTokenIsInvalid_DoesNotCheckUserRepository()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();

        _providerValidator
            .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(cmd.Provider, cmd.ProviderToken))
            .ReturnsAsync((string?)null);

        await sut.Handle(cmd, CancellationToken.None);

        _userRepository.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<AppUser>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProviderTokenIsInvalid_DoesNotCallCoordinator()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();

        _providerValidator
            .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(cmd.Provider, cmd.ProviderToken))
            .ReturnsAsync((string?)null);

        await sut.Handle(cmd, CancellationToken.None);

        _coordinator.Verify(
            x => x.RegisterAsync(It.IsAny<ProviderRegisterCommand>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProviderTokenReturnsEmptyEmail_ReturnsError()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();

        _providerValidator
            .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(cmd.Provider, cmd.ProviderToken))
            .ReturnsAsync(string.Empty);

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid provider token", result.Errors);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyExists_ReturnsConflict()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var email = "existing@example.com";

        _providerValidator
            .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(cmd.Provider, cmd.ProviderToken))
            .ReturnsAsync(email);
        SetupUserByEmail(new AppUser { Email = email });

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
        var email = "existing@example.com";

        _providerValidator
            .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(cmd.Provider, cmd.ProviderToken))
            .ReturnsAsync(email);
        SetupUserByEmail(new AppUser { Email = email });

        await sut.Handle(cmd, CancellationToken.None);

        _coordinator.Verify(
            x => x.RegisterAsync(It.IsAny<ProviderRegisterCommand>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_CallsCoordinatorWithCommandAndEmail()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var email = "new@example.com";

        _providerValidator
            .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(cmd.Provider, cmd.ProviderToken))
            .ReturnsAsync(email);
        SetupUserByEmail(null);
        _coordinator
            .Setup(x => x.RegisterAsync(cmd, email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProviderRegisterResult>.Success(new ProviderRegisterResult()));

        await sut.Handle(cmd, CancellationToken.None);

        _coordinator.Verify(x => x.RegisterAsync(cmd, email, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ReturnsCoordinatorSuccessResult()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var email = "new@example.com";
        var expected = Result<ProviderRegisterResult>.Success(new ProviderRegisterResult());

        _providerValidator
            .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(cmd.Provider, cmd.ProviderToken))
            .ReturnsAsync(email);
        SetupUserByEmail(null);
        _coordinator
            .Setup(x => x.RegisterAsync(cmd, email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.Same(expected, result);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WhenCoordinatorReturnsError_PropagatesError()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var email = "new@example.com";
        var errorResult = Result<ProviderRegisterResult>.Error("Registration failed");

        _providerValidator
            .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(cmd.Provider, cmd.ProviderToken))
            .ReturnsAsync(email);
        SetupUserByEmail(null);
        _coordinator
            .Setup(x => x.RegisterAsync(cmd, email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorResult);

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Registration failed", result.Errors);
    }

    [Fact]
    public async Task Handle_WhenCoordinatorThrows_ExceptionPropagates()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var email = "new@example.com";

        _providerValidator
            .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(cmd.Provider, cmd.ProviderToken))
            .ReturnsAsync(email);
        SetupUserByEmail(null);
        _coordinator
            .Setup(x => x.RegisterAsync(cmd, email, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(cmd, CancellationToken.None));
        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var email = "new@example.com";
        using var cts = new CancellationTokenSource();

        _providerValidator
            .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(cmd.Provider, cmd.ProviderToken))
            .ReturnsAsync(email);
        SetupUserByEmail(null);
        _coordinator
            .Setup(x => x.RegisterAsync(cmd, email, cts.Token))
            .ReturnsAsync(Result<ProviderRegisterResult>.Success(new ProviderRegisterResult()));

        await sut.Handle(cmd, cts.Token);

        _userRepository.Verify(
            x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                cts.Token),
            Times.Once);
        _coordinator.Verify(x => x.RegisterAsync(cmd, email, cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDifferentProviders_Succeeds()
    {
        var sut = CreateSut();

        foreach (var provider in Enum.GetValues<AuthProvider>())
        {
            var cmd = CreateCommand(provider: provider);
            var email = $"user@provider-{provider}.com";

            _providerValidator
                .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(provider, cmd.ProviderToken))
                .ReturnsAsync(email);
            SetupUserByEmail(null);
            _coordinator
                .Setup(x => x.RegisterAsync(cmd, email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProviderRegisterResult>.Success(new ProviderRegisterResult()));

            var result = await sut.Handle(cmd, CancellationToken.None);

            Assert.True(result.IsSuccess);
        }
    }

    [Fact]
    public async Task Handle_UsesUserByEmailSpec_ToCheckExistingUser()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var email = "test@example.com";
        ISpecification<AppUser>? capturedSpec = null;

        _providerValidator
            .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(cmd.Provider, cmd.ProviderToken))
            .ReturnsAsync(email);
        _userRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                It.IsAny<CancellationToken>()))
            .Callback<ISpecification<AppUser>, CancellationToken>((spec, _) => capturedSpec = spec)
            .ReturnsAsync((AppUser?)null);
        _coordinator
            .Setup(x => x.RegisterAsync(cmd, email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProviderRegisterResult>.Success(new ProviderRegisterResult()));

        await sut.Handle(cmd, CancellationToken.None);

        Assert.NotNull(capturedSpec);
        Assert.IsType<UserByEmailSpec>(capturedSpec);
    }

    [Fact]
    public async Task Handle_PassesCorrectProviderAndTokenToValidator()
    {
        var sut = CreateSut();
        var providerToken = "specific-token";
        var provider = AuthProvider.Google;
        var cmd = CreateCommand(providerToken: providerToken, provider: provider);
        var email = "test@example.com";

        _providerValidator
            .Setup(x => x.ValidateProviderTokenAndGetEmailAsync(provider, providerToken))
            .ReturnsAsync(email);
        SetupUserByEmail(null);
        _coordinator
            .Setup(x => x.RegisterAsync(cmd, email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProviderRegisterResult>.Success(new ProviderRegisterResult()));

        await sut.Handle(cmd, CancellationToken.None);

        _providerValidator.Verify(
            x => x.ValidateProviderTokenAndGetEmailAsync(provider, providerToken),
            Times.Once);
    }
}
