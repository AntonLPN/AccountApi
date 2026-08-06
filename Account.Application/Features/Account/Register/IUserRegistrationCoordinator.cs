using Ardalis.Result;

namespace Account.Application.Features.Account.Register;

public interface IUserRegistrationCoordinator
{
    Task<Result<RegisterUserResult>> RegisterAsync(RegisterCommand request, CancellationToken ct);
}   
