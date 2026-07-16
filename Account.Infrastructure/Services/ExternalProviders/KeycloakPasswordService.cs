using Account.Domain.Interfaces;
using Account.Infrastructure.Configuration;
using Account.Infrastructure.HttpClients;
using Ardalis.Result;
using Microsoft.Extensions.Options;

namespace Account.Infrastructure.Services.ExternalProviders;

public class KeycloakPasswordService(
    KeycloakHttpClient keycloakHttpClient,
    IOptions<KeycloakAdminOptions> keyCloakOptions) : IPasswordService
{
    public Task<Result> ChangePasswordAsync(string email, string newPassword)
    {
        ArgumentException.ThrowIfNullOrEmpty(email);
        ArgumentException.ThrowIfNullOrEmpty(newPassword);
        return keycloakHttpClient.ChangePasswordByEmailAsync(email, newPassword, keyCloakOptions.Value);
    }
}