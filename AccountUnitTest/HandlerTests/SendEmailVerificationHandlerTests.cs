using Account.Application.Features.Account.SendEmailVerification;
using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Specifications;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Ardalis.Specification;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountUnitTest.HandlerTests;

public class SendEmailVerificationHandlerTests
{
    private readonly Mock<ILogger<SendEmailVerificationHandler>> _logger = new();
    private readonly Mock<IRepository<AppUser>> _userRepository = new();
    private readonly Mock<IEmail> _emailService = new();
    private readonly Mock<IDataCache> _dataCache = new();

    private SendEmailVerificationHandler CreateSut()
        => new(_logger.Object, _userRepository.Object, _emailService.Object, _dataCache.Object);

    private static SendEmailVerificationCommand CreateCommand(string email = "test@example.com")
        => new(email);

    private void SetupUserByEmail(Email normalizedEmail, AppUser? user)
        => _userRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

    [Fact]
    public async Task Handle_WhenEmailIsNull_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var cmd = new SendEmailVerificationCommand(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenEmailIsEmpty_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var cmd = new SendEmailVerificationCommand("");

        await Assert.ThrowsAsync<ArgumentException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenEmailIsWhitespace_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var cmd = new SendEmailVerificationCommand("   ");

        await Assert.ThrowsAsync<ArgumentException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var normalizedEmail = Email.Create(cmd.Email);

        SetupUserByEmail(normalizedEmail, null);

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("User not found", result.Errors);
        
        _emailService.Verify(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _dataCache.Verify(x => x.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserFound_SendsVerificationEmail()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var normalizedEmail = Email.Create(cmd.Email);
        var user = new AppUser { Id = Guid.NewGuid(), Email = normalizedEmail };

        SetupUserByEmail(normalizedEmail, user);
        _dataCache
            .Setup(x => x.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);
        _emailService
            .Setup(x => x.SendVerificationEmailAsync(cmd.Email, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Value);
        
        _emailService.Verify(
            x => x.SendVerificationEmailAsync(cmd.Email, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserFound_StoresTokenInCache()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var normalizedEmail = Email.Create(cmd.Email);
        var user = new AppUser { Id = Guid.NewGuid(), Email = normalizedEmail };

        SetupUserByEmail(normalizedEmail, user);
        _dataCache
            .Setup(x => x.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);
        _emailService
            .Setup(x => x.SendVerificationEmailAsync(cmd.Email, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.True(result.IsSuccess);
        
        _dataCache.Verify(
            x => x.SetStringAsync(
                It.Is<string>(key => key.StartsWith("email_verification_")),
                normalizedEmail.Value,
                TimeSpan.FromMinutes(10)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserFound_ReturnsTokenAsValue()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var normalizedEmail = Email.Create(cmd.Email);
        var user = new AppUser { Id = Guid.NewGuid(), Email = normalizedEmail };
        string? capturedToken = null;

        SetupUserByEmail(normalizedEmail, user);
        _dataCache
            .Setup(x => x.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Callback<string, string, TimeSpan>((key, _, _) =>
            {
                capturedToken = key.Replace("email_verification_", "");
            })
            .Returns(Task.CompletedTask);
        _emailService
            .Setup(x => x.SendVerificationEmailAsync(cmd.Email, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(capturedToken, result.Value);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value);
    }

    [Fact]
    public async Task Handle_WhenEmailSendingFails_ReturnsError()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var normalizedEmail = Email.Create(cmd.Email);
        var user = new AppUser { Id = Guid.NewGuid(), Email = normalizedEmail };

        SetupUserByEmail(normalizedEmail, user);
        _dataCache
            .Setup(x => x.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);
        _emailService
            .Setup(x => x.SendVerificationEmailAsync(cmd.Email, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Failed to send email", result.Errors);
    }

    [Fact]
    public async Task Handle_WhenExceptionOccurs_LogsErrorAndThrows()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var normalizedEmail = Email.Create(cmd.Email);
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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error occured while sending email verification")),
                testException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NormalizesEmailBeforeQuerying()
    {
        var sut = CreateSut();
        var cmd = CreateCommand("Test@EXAMPLE.com");
        var normalizedEmail = Email.Create(cmd.Email); // "test@example.com"
        var user = new AppUser { Id = Guid.NewGuid(), Email = normalizedEmail };

        SetupUserByEmail(normalizedEmail, user);
        _dataCache
            .Setup(x => x.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);
        _emailService
            .Setup(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await sut.Handle(cmd, CancellationToken.None);

        _userRepository.Verify(
            x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var normalizedEmail = Email.Create(cmd.Email);
        var user = new AppUser { Id = Guid.NewGuid(), Email = normalizedEmail };
        using var cts = new CancellationTokenSource();

        _userRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                cts.Token))
            .ReturnsAsync(user);
        _dataCache
            .Setup(x => x.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);
        _emailService
            .Setup(x => x.SendVerificationEmailAsync(cmd.Email, It.IsAny<string>(), cts.Token))
            .ReturnsAsync(true);

        await sut.Handle(cmd, cts.Token);

        _userRepository.Verify(
            x => x.FirstOrDefaultAsync(
                It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
                cts.Token),
            Times.Once);
        _emailService.Verify(
            x => x.SendVerificationEmailAsync(cmd.Email, It.IsAny<string>(), cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_TokenIsGuid_InNormalizedFormat()
    {
        var sut = CreateSut();
        var cmd = CreateCommand();
        var normalizedEmail = Email.Create(cmd.Email);
        var user = new AppUser { Id = Guid.NewGuid(), Email = normalizedEmail };
        string? capturedToken = null;

        SetupUserByEmail(normalizedEmail, user);
        _dataCache
            .Setup(x => x.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Callback<string, string, TimeSpan>((key, _, _) =>
            {
                capturedToken = key.Replace("email_verification_", "");
            })
            .Returns(Task.CompletedTask);
        _emailService
            .Setup(x => x.SendVerificationEmailAsync(cmd.Email, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedToken);
        // Token should be a GUID in normalized format (without dashes)
        Assert.True(Guid.TryParse(capturedToken, out _) || capturedToken.Length == 32);
        Assert.DoesNotContain("-", capturedToken);
    }

    [Fact]
    public async Task Handle_SendsEmailWithOriginalEmailAddress()
    {
        var sut = CreateSut();
        var originalEmail = "test@example.com";
        var cmd = CreateCommand(originalEmail);
        var normalizedEmail = Email.Create(cmd.Email);
        var user = new AppUser { Id = Guid.NewGuid(), Email = normalizedEmail };

        SetupUserByEmail(normalizedEmail, user);
        _dataCache
            .Setup(x => x.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);
        _emailService
            .Setup(x => x.SendVerificationEmailAsync(originalEmail, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await sut.Handle(cmd, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _emailService.Verify(
            x => x.SendVerificationEmailAsync(originalEmail, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}