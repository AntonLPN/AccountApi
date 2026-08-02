// using Account.Application.Features.Account.Login;
// using Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;
// using Account.Domain.Entities;
// using Account.Domain.Interfaces;
// using Account.Domain.Models;
// using Account.Domain.Repositories;
// using Account.Domain.Specifications;
// using Ardalis.Result;
// using Ardalis.SharedKernel;
// using Ardalis.Specification;
// using MassTransit;
// using Microsoft.Extensions.Logging;
// using Moq;
//
// namespace AccountUnitTest.HandlerTests;
//
// public class LoginUserHandlerTests
// {
//     private readonly Mock<ILogger<LoginUserHandler>> _logger = new();
//     private readonly Mock<IAuthService> _authService = new();
//     private readonly Mock<IUnitOfWork> _unitOfWork = new();
//     private readonly Mock<IRepository<AppUser>> _userRepository = new();
//     private readonly Mock<IRepository<ApiKey>> _apiKeyRepository = new();
//     private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
//     private readonly Mock<IMfaManager> _mfaManager = new();
//     private readonly Mock<IPreAuthTokenService> _preAuthTokenService = new();
//
//     private LoginUserHandler CreateSut()
//         => new(
//             _logger.Object,
//             _authService.Object,
//             _unitOfWork.Object,
//             _userRepository.Object,
//             _publishEndpoint.Object,
//             _mfaManager.Object,
//             _preAuthTokenService.Object);
//
//     private static LoginCommand CreateCommand(string email = "test@mail.com",
//         string password = "123Avc_!@#$%^&*()_+", string? ipAddress = "127.0.0.1", string? userAgent = "userAgent")
//         => new(email, password, ipAddress, userAgent);
//
//     private static TokenResponse CreateTokenResponse() => new()
//     {
//         AccessToken = "access_token",
//         RefreshToken = "refresh_token",
//         TokenType = "token_type",
//         ExpiresIn = 3600,
//         Scope = "scope"
//     };
//
//     [Fact]
//     public async Task Handle_WhenAuthServiceReturnsNull_ReturnsUnauthorized()
//     {
//         var sut = CreateSut();
//         var cmd = CreateCommand();
//
//         //Arrange
//         _authService
//             .Setup(x => x.LoginAsync(cmd.Email, cmd.Password))
//             .ReturnsAsync((TokenResponse?)null);
//
//         //Act
//         var result = await sut.Handle(cmd, CancellationToken.None);
//
//         //Assert
//         Assert.False(result.IsSuccess);
//         Assert.Equal(ResultStatus.Unauthorized, result.Status);
//
//         _userRepository.Verify(x => x.FirstOrDefaultAsync(
//             It.IsAny<ISpecification<AppUser>>(), It.IsAny<CancellationToken>()), Times.Never);
//         _publishEndpoint.Verify(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
//     }
//
//     [Fact]
//     public async Task Handle_WhenUserNotFound_ReturnsUnauthorized()
//     {
//         var sut = CreateSut();
//         var cmd = CreateCommand();
//
//         //Arrange
//         _authService
//             .Setup(x => x.LoginAsync(cmd.Email, cmd.Password))
//             .ReturnsAsync(CreateTokenResponse());
//
//         _userRepository
//             .Setup(x => x.FirstOrDefaultAsync(
//                 It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
//                 It.IsAny<CancellationToken>()))
//             .ReturnsAsync((AppUser?)null);
//
//         //Act
//         var result = await sut.Handle(cmd, CancellationToken.None);
//
//         //Assert
//         Assert.False(result.IsSuccess);
//         Assert.Equal(ResultStatus.Unauthorized, result.Status);
//
//         _apiKeyRepository.Verify(x => x.FirstOrDefaultAsync(
//             It.IsAny<ISpecification<ApiKey>>(), It.IsAny<CancellationToken>()), Times.Never);
//         _mfaManager.Verify(x => x.InitiateTwoFactorProcessAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()),
//             Times.Never);
//     }
//
//     [Fact]
//     public async Task Handle_WhenTwoFactorEnabled_ReturnsMfaRequired()
//     {
//         var sut = CreateSut();
//         var cmd = CreateCommand();
//         var user = new AppUser { Id = "user-id", Email = cmd.Email, IsTwoFactorEnabled = true };
//         const string preAuthToken = "pre-auth-token";
//
//         //Arrange
//         _authService
//             .Setup(x => x.LoginAsync(cmd.Email, cmd.Password))
//             .ReturnsAsync(CreateTokenResponse());
//
//         _userRepository
//             .Setup(x => x.FirstOrDefaultAsync(
//                 It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
//                 It.IsAny<CancellationToken>()))
//             .ReturnsAsync(user);
//
//         _preAuthTokenService
//             .Setup(x => x.GeneratePreAuthToken(cmd.Email))
//             .Returns(preAuthToken);
//
//         _mfaManager
//             .Setup(x => x.InitiateTwoFactorProcessAsync(user, It.IsAny<CancellationToken>()))
//             .ReturnsAsync(string.Empty);
//
//         //Act
//         var result = await sut.Handle(cmd, CancellationToken.None);
//
//         //Assert
//         Assert.True(result.IsSuccess);
//         Assert.NotNull(result.Value);
//         Assert.True(result.Value.IsMfaRequired);
//         Assert.NotNull(result.Value.Token);
//         Assert.Equal(preAuthToken, result.Value.Token.AccessToken);
//         Assert.Equal("pre-auth", result.Value.Token.TokenType);
//         Assert.Equal(string.Empty, result.Value.Token.RefreshToken);
//         Assert.Equal(0, result.Value.Token.ExpiresIn);
//
//         _mfaManager.Verify(x => x.InitiateTwoFactorProcessAsync(user, It.IsAny<CancellationToken>()), Times.Once);
//         _apiKeyRepository.Verify(x => x.FirstOrDefaultAsync(
//             It.IsAny<ISpecification<ApiKey>>(), It.IsAny<CancellationToken>()), Times.Never);
//         _publishEndpoint.Verify(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
//         _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
//     }
//     
//     [Fact]
//     public async Task Handle_WhenTwoFactorDisabledAndApiKeyFound_ReturnsSuccess()
//     {
//         var sut = CreateSut();
//         var cmd = CreateCommand();
//         var user = new AppUser { Id = "user-id", Email = cmd.Email, IsTwoFactorEnabled = false };
//         var apiKey = new ApiKey { Id = 1, UserId = user.Id, ApiKeyValue = "api_key_value" };
//         var tokenResponse = CreateTokenResponse();
//
//         //Arrange
//         _authService
//             .Setup(x => x.LoginAsync(cmd.Email, cmd.Password))
//             .ReturnsAsync(tokenResponse);
//
//         _userRepository
//             .Setup(x => x.FirstOrDefaultAsync(
//                 It.Is<ISpecification<AppUser>>(s => s is UserByEmailSpec),
//                 It.IsAny<CancellationToken>()))
//             .ReturnsAsync(user);
//
//         _apiKeyRepository
//             .Setup(x => x.FirstOrDefaultAsync(
//                 It.Is<ISpecification<ApiKey>>(s => s is ApiKeyByUserIdSpec),
//                 It.IsAny<CancellationToken>()))
//             .ReturnsAsync(apiKey);
//
//         _publishEndpoint
//             .Setup(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
//             .Returns(Task.CompletedTask);
//
//         //Act
//         var result = await sut.Handle(cmd, CancellationToken.None);
//
//         //Assert
//         Assert.True(result.IsSuccess);
//         Assert.Equal(ResultStatus.Ok, result.Status);
//         Assert.NotNull(result.Value);
//         Assert.False(result.Value.IsMfaRequired);
//         Assert.Equal(apiKey.ApiKeyValue, result.Value.ApiKeys);
//         Assert.Equal(tokenResponse.AccessToken, result.Value.Token!.AccessToken);
//
//         _publishEndpoint.Verify(x => x.Publish(
//             It.Is<UserLoginSagaStartedIntegrationEvent>(e =>
//                 e.UserId == user.Id &&
//                 e.Email == user.Email &&
//                 e.IpAddress == cmd.IpAddress &&
//                 e.UserAgent == cmd.UserAgent),
//             It.IsAny<CancellationToken>()), Times.Once);
//
//         _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
//         _mfaManager.Verify(x => x.InitiateTwoFactorProcessAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()),
//             Times.Never);
//     }
//
//     [Fact]
//     public async Task Handle_WhenAuthServiceThrows_LogsAndRethrows()
//     {
//         var sut = CreateSut();
//         var cmd = CreateCommand();
//         var expectedException = new InvalidOperationException("boom");
//
//         //Arrange
//         _authService
//             .Setup(x => x.LoginAsync(cmd.Email, cmd.Password))
//             .ThrowsAsync(expectedException);
//
//         //Act & Assert
//         var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
//             () => sut.Handle(cmd, CancellationToken.None));
//         Assert.Same(expectedException, thrown);
//
//         _logger.Verify(x => x.Log(
//             LogLevel.Error,
//             It.IsAny<EventId>(),
//             It.IsAny<It.IsAnyType>(),
//             expectedException,
//             It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
//             Times.Once);
//     }
// }