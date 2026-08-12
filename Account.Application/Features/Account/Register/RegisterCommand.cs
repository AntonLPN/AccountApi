using Account.Domain.Enums;
using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.Register;

public record RegisterCommand(
    AuthProvider Provider,
    string Email,
    bool EmailConfirmed,
    string Password,
    string ReferrerCode,
    string? IpAddress,
    string? UserAgent)
    : ICommand<Result<RegisterUserResult>>;