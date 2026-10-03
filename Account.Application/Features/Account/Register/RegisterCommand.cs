using Account.Domain.Enums;
using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.Register;

public record RegisterCommand(
    AuthProvider Provider,
    string Email,
    bool EmailConfirmed,
    string Password,
    string? Name,
    string? Surname,
    string? ReferrerCode,
    string? IpAddress,
    string? UserAgent,
    string PhoneNumber)
    : ICommand<Result<RegisterUserResult>>;