using System.Text.Json;
using Account.Domain.Entities;
using Account.Domain.Specifications;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.AccountInfo;

public class AccountInfoHandler(ILogger<AccountInfoHandler> logger, IRepository<AppUser> userRepository)
    : ICommandHandler<AccountInfoCommand, Result<AccountInfoResult>>
{
    public async Task<Result<AccountInfoResult>> Handle(AccountInfoCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                logger.LogWarning("Email is null or empty in AccountInfoCommand");
                return Result<AccountInfoResult>.Invalid(new ValidationError("Email", "Email cannot be null or empty"));
            }

            var user = await userRepository.FirstOrDefaultAsync(new UserByEmailSpec(request.Email), cancellationToken);
            if (user is null)
                return Result<AccountInfoResult>.NotFound("User not found");
            var result = new AccountInfoResult
            {
                Email = user.Email,
                Type = GetTypeFromMetadata(user.MetadataJson),
                Name = user.Name,
                Surname = user.Surname,
                EmailConfirmed = user.EmailConfirmed,
                ReferralCode = user.ReferralCode,
                IsTwoFactorEnabled = user.IsTwoFactorEnabled,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Metadata = user.MetadataJson ?? ""
            };

            return Result<AccountInfoResult>.Success(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error handling AccountInfoCommand for email {Email}", MaskedEmail.Create(request.Email));
            throw;
        }
    }

    private static string GetTypeFromMetadata(string? metadataJson)
    {
        if (metadataJson is null)
            return "Role";

        using var metadata = JsonDocument.Parse(metadataJson);
        foreach (var property in metadata.RootElement.EnumerateObject())
        {
            if (string.Equals(property.Name, "Type", StringComparison.OrdinalIgnoreCase) &&
                property.Value.ValueKind == JsonValueKind.String)
            {
                return property.Value.GetString() ?? "Role";
            }
        }

        return "Role";
    }
}