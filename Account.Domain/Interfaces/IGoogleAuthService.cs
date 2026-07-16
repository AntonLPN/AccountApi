using Account.Domain.Models;

namespace Account.Domain.Interfaces;

public interface IGoogleAuthService
{
    Task<GooglePayload> ValidateTokenAsync(string googleToken);
}