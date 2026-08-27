using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.ConfirmEmail;

public record ConfirmEmailCommand(string Email, string Token) : ICommand<Result<bool>>;