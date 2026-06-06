using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.Logout;

public record LogoutCommand(
    string Email,
    string RefreshToken,
    string? IpAddress,
    string? UserAgent) : ICommand<Result>;