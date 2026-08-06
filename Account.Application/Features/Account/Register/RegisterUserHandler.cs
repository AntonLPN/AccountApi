using Account.Domain.Entities;
using Account.Domain.Specifications;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.Register;

public class RegisterUserHandler(ILogger<RegisterUserHandler> logger,
    IRepository<AppUser> userRepository,
    IUserRegistrationCoordinator coordinator)
    : ICommandHandler<RegisterCommand, Result<RegisterUserResult>>
{
    public async Task<Result<RegisterUserResult>> Handle(RegisterCommand request, CancellationToken ct)
    {
        logger.LogInformation("Registering user: {Email}", MaskedEmail.Create(request.Email));
        var normalizedEmail = Email.Create(request.Email);
        var existing = await userRepository.FirstOrDefaultAsync(new UserByEmailSpec(normalizedEmail), ct);
        if (existing is not null)
            return Result<RegisterUserResult>.Conflict("User already exists");

        return await coordinator.RegisterAsync(request, ct);
    }

}