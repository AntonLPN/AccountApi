using Account.Application.Features.Account.AccountInfo;
using Account.Domain.Entities;
using Ardalis.Specification;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountUnitTest.HandlerTests;

public class AccountInfoHandlerTests
{
    [Fact]
    public async Task Handle_WhenMetadataContainsType_ReturnsMetadataType()
    {
        var repository = new Mock<IRepository<AppUser>>();
        repository
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<AppUser>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppUser
            {
                Email = "type@example.com",
                MetadataJson = """{"Type":"Partner"}"""
            });
        var handler = new AccountInfoHandler(
            Mock.Of<ILogger<AccountInfoHandler>>(),
            repository.Object);

        var result = await handler.Handle(new AccountInfoCommand("type@example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Partner", result.Value.Type);
    }

    [Fact]
    public async Task Handle_WhenMetadataDoesNotContainType_ReturnsDefaultType()
    {
        var repository = new Mock<IRepository<AppUser>>();
        repository
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<AppUser>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppUser
            {
                Email = "type@example.com",
                MetadataJson = """{"source":"registration-form"}"""
            });
        var handler = new AccountInfoHandler(
            Mock.Of<ILogger<AccountInfoHandler>>(),
            repository.Object);

        var result = await handler.Handle(new AccountInfoCommand("type@example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Role", result.Value.Type);
    }
}
