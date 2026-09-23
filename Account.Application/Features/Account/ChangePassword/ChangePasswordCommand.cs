using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.ChangePassword;

public record ChangePasswordCommand(string Email, string Password, string PendingToken)
    : ICommand<Result<ChangePasswordResult>>;