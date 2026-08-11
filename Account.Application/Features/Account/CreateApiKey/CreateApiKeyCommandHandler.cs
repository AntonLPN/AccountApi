using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Specifications;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.CreateApiKey;

public class CreateApiKeyCommandHandler(
    ILogger<CreateApiKeyCommandHandler> logger,
    IRepository<AppUser> userRepository,
    IRepository<ApiKey> apiKeyRepository,
    ICryptography cryptographyService)
    : ICommandHandler<CreateApiKeyCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateApiKeyCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = Email.Create(request.Email);
        try
        {
            var user = await userRepository.FirstOrDefaultAsync(new UserByEmailSpec(normalizedEmail),
                cancellationToken);
            if (user is null)
                return Result<string>.NotFound("User not found");
            var key = Guid.NewGuid().ToString("N");
            var hashedKey = cryptographyService.Hash(key); 
            var apiKey = ApiKey.Create(new ApiKeyCreateParams(user.Id,key,hashedKey,true));
            await apiKeyRepository.AddAsync(apiKey, cancellationToken);
            return Result<string>.Success(key);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred while handling CreateApiKeyCommand");
            throw;
        }
    }
}