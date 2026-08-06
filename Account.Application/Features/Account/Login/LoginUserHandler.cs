using Account.Application.Interfaces;
using Account.Domain.Entities;
using Account.Domain.Specifications;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.Login;

public class LoginUserHandler(
    IRepository<AppUser> userRepository,
    IEnumerable<ILoginStrategy> loginStrategies)
    : ICommandHandler<LoginCommand, Result<LoginUserResult>>
{
    public async Task<Result<LoginUserResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = Email.Create(request.Email);
        var user = await userRepository.FirstOrDefaultAsync(
            new UserByEmailWithAuthorizedApiKeysSpec(normalizedEmail), cancellationToken);
        
        if (user is null)
            return Result<LoginUserResult>.NotFound("User not found");
        var strategy = loginStrategies.First(s => s.CanHandle(user));
        return await strategy.HandleAsync(user, request, cancellationToken);
        
     
    }
    
}