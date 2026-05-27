using Account.Domain.Interfaces;
using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.Register;

public class RegisterUserHandler(IAuthService authService)
    : ICommandHandler<RegisterCommand, Result<RegisterUserResult>>
{
    public async Task<Result<RegisterUserResult>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterUserAsync(request.Email, request.Password);
        if (result is null)
            return Result<RegisterUserResult>.Error("Registration failed");
        //TODO : Implement RegisterUserHandler logic
        //TODO implement transaction logic for save user and api key to db
        throw new NotImplementedException();
    }
}