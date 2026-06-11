using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.Authentication;

public record AuthenticationCommand(string RefreshToken) : ICommand<Result<AuthenticationResult>>;