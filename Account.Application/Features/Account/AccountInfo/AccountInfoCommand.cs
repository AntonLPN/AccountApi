using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.AccountInfo;

public record AccountInfoCommand(string Email) : ICommand<Result<AccountInfoResult>>;