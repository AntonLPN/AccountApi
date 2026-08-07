using Account.Application.Interfaces;
using Account.Domain.Entities;
using Account.Domain.Specifications;
using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.ProvidersRegister;

public class ProviderRegisterHandler(
    IProviderValidator providerValidator,
    IRepository<AppUser> userRepository,
    IProviderRegistrationCoordinator coordinator)
    : ICommandHandler<ProviderRegisterCommand, Result<ProviderRegisterResult>>
{
    public async Task<Result<ProviderRegisterResult>> Handle(ProviderRegisterCommand request, CancellationToken ct)
    {
        var email = await providerValidator.ValidateProviderTokenAndGetEmailAsync(request.Provider,
            request.ProviderToken);
        if (string.IsNullOrEmpty(email))
            return Result<ProviderRegisterResult>.Error("Invalid provider token");

        if (await userRepository.FirstOrDefaultAsync(new UserByEmailSpec(email), ct) is not null)
            return Result<ProviderRegisterResult>.Conflict("User already exists");

        return await coordinator.RegisterAsync(request, email, ct);
    }
}

