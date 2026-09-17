using Account.Application.Features.Account.IsEmailVerified;
using Account.Domain.Entities;
using Account.Domain.Specifications;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Ardalis.Specification;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountUnitTest.HandlerTests;

public class IsEmailVerifiedHandlerTests
{
    private readonly Mock<ILogger<IsEmailVerifiedHandler>> _logger = new();
    private readonly Mock<IRepository<AppUser>> _userRepository = new();

    private IsEmailVerifiedHandler CreateSut()
        => new(_logger.Object, _userRepository.Object);

    private static IsEmailVerifiedCommand CreateCommand(string email = "test@example.com")
        => new(email);

    private void SetupUserByEmail(AppUser? user)
        => _userRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenEmailIsMissing_ReturnsInvalid(string? email)
    {
        var sut = CreateSut();

        var result = await sut.Handle(new IsEmailVerifiedCommand(email!), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        _userRepository.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<AppUser>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmailFormatIsInvalid_ReturnsInvalid()
    {
        var sut = CreateSut();

        var result = await sut.Handle(CreateCommand("not-an-email"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        _userRepository.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<AppUser>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        var sut = CreateSut();
        SetupUserByEmail(null);

        var result = await sut.Handle(CreateCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("User not found", result.Errors);
    }

    [Fact]
    public async Task Handle_WhenEmailConfirmed_ReturnsVerifiedTrue()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var normalizedEmail = Email.Create(cmd.Email);
        SetupUserByEmail(new AppUser { Id = Guid.NewGuid(), Email = normalizedEmail, EmailConfirmed = true });

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.True(result.Value.IsEmailVerified);
        Assert.Equal(MaskedEmail.Create(normalizedEmail), MaskedEmail.Create(result.Value.Email));
    }

    [Fact]
    public async Task Handle_WhenEmailNotConfirmed_ReturnsVerifiedFalse()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var normalizedEmail = Email.Create(cmd.Email);
        SetupUserByEmail(new AppUser { Id = Guid.NewGuid(), Email = normalizedEmail, EmailConfirmed = false });

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsEmailVerified);
    }

    [Fact]
    public async Task Handle_NormalizesEmailBeforeQuerying()
    {
        var sut = CreateSut();
        var cmd = CreateCommand("Test@EXAMPLE.com");
        var normalizedEmail = Email.Create(cmd.Email); // "test@example.com"
        SetupUserByEmail(new AppUser { Id = Guid.NewGuid(), Email = normalizedEmail, EmailConfirmed = true });

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(MaskedEmail.Create("test@example.com") ,MaskedEmail.Create(result.Value.Email));
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        using var cts = new CancellationTokenSource();

        _userRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                cts.Token))
            .ReturnsAsync(new AppUser { Id = Guid.NewGuid(), Email = Email.Create(cmd.Email) });

        await sut.Handle(cmd, cts.Token);

        _userRepository.Verify(
            x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExceptionOccurs_LogsErrorAndThrows()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var testException = new InvalidOperationException("Database error");

        _userRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(testException);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(cmd, CancellationToken.None));

        Assert.Equal(testException, ex);
        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("Error occurred while handling IsEmailVerifiedCommand")),
                testException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
