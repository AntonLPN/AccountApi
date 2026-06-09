using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.Register;

public record RegisterCommand(string Email, string Password, string ReferrerCode) : ICommand<Result<RegisterUserResult>>;