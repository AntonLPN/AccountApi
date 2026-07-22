using Account.Application.Features.Account.ChangePassword;
using Account.Application.Features.Account.ForgotPassword;
using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountUnitTest.HandlerTests;

public class ForgotPasswordHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new Mock<IUserRepository>();
    private readonly Mock<IMfaManager> _twoFactorManagerMock = new Mock<IMfaManager>();
    private readonly Mock<IPreAuthTokenService> _preAuthTokenServiceMock = new Mock<IPreAuthTokenService>();
    private readonly ForgotPasswordHandler _forgotPasswordHandler;

    
    public ForgotPasswordHandlerTests()
    {
        var logger = LoggerFactory.Create(builder =>
            builder.AddConsole());

        _forgotPasswordHandler = new ForgotPasswordHandler(
            logger.CreateLogger<ForgotPasswordHandler>(),
            _userRepositoryMock.Object,
            _twoFactorManagerMock.Object,
            _preAuthTokenServiceMock.Object);
    }
    
    
    
    [Fact]
    public async Task Handle_ReturnsNotFoundWhenUserNotFound()
    {
        var request = new ForgotPasswordCommand ("user@example.com");
        _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(request.Email, CancellationToken.None))
            .ReturnsAsync((AppUser?)null);

        var result = await _forgotPasswordHandler.Handle(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("User not found", result.Errors.First());
    }
}