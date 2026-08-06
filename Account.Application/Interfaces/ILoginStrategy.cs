using Account.Application.Features.Account.Login;
using Account.Domain.Entities;
using Ardalis.Result;

namespace Account.Application.Interfaces;

public interface ILoginStrategy
{
    bool CanHandle(AppUser user);
    Task<Result<LoginUserResult>> HandleAsync(
        AppUser user, LoginCommand request, CancellationToken ct);
}