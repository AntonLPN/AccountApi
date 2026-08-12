using Ardalis.Result;

namespace Account.Application.Features.Account.Register;

public interface IUserRegistrationCoordinator
{
    Task<Result<RegisterUserResult>> RegisterAsync(UserCoordinatorParams request, CancellationToken ct);
}   
