using Account.Domain.Entities;
using Account.Domain.Specifications;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.DeleteApiKey;

public class DeleteApiKeyHandler(
    ILogger<DeleteApiKeyHandler> logger,
    IRepository<AppUser> userRepository,
    IRepository<ApiKey> apiKeyRepository) : ICommandHandler<DeleteApiKeyCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteApiKeyCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = Email.Create(request.Email);
        try
        {
            var user = await userRepository.FirstOrDefaultAsync(new UserByEmailSpec(normalizedEmail),
                cancellationToken);
            if (user is null)
                return Result<bool>.NotFound("User not found");
            var apiKey =
                await apiKeyRepository.FirstOrDefaultAsync(new ApiKeyByValueSpec(request.ApiKey), cancellationToken);
            if (apiKey is null)
                return Result<bool>.NotFound("Api key not found");
            apiKey.Revoke();
            await apiKeyRepository.UpdateAsync(apiKey, cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred while handling DeleteApiKeyCommand for email {Email}",
                MaskedEmail.Create(normalizedEmail));
            throw;
        }
    }
}