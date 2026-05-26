using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.Register;

public class RegisterUserHandler:ICommandHandler<RegisterCommand,Result<RegisterUserResult>>
{
    public Task<Result<RegisterUserResult>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        //TODO : Implement RegisterUserHandler logic
        throw new NotImplementedException();
    }
}