using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.Login;

public record LoginCommand(
    string Email,
    string Password,
    string? IpAddress,
    string? UserAgent) : ICommand<Result<LoginUserResult>>;