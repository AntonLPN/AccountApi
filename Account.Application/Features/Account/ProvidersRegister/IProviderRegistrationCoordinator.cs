using Ardalis.Result;

namespace Account.Application.Features.Account.ProvidersRegister;

public interface IProviderRegistrationCoordinator
{
    Task<Result<ProviderRegisterResult>> RegisterAsync(ProviderRegisterCommand request,string email, CancellationToken ct);
}