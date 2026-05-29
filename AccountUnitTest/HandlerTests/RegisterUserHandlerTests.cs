using Account.Application.Features.Account.Register;
using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Repositories;
using Ardalis.Result;
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
            _cryptographyService.Object);
    }

    private static RegisterCommand CreateCommand(string email = "test@mail.com",
        string password = "123Avc_!@#$%^&*()_+")
        => new(email, password);

    [Fact]
    public async Task Handle_WhenEmailExists_ReturnsConflict()
    {
        var sut = CreateSut();
        var command = CreateCommand();
        _userRepository.Setup(x => x.GetUserByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppUser());
        var result = await sut.Handle(command, CancellationToken.None);
        //Assets
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains("User already exists", result.Errors);
    }
}