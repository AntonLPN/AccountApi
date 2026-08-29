using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.ConfirmEmail;

public record ConfirmEmailCommand(string Token) : ICommand<Result<bool>>;