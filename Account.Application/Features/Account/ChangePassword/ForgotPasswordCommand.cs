using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.ChangePassword;

public record ForgotPasswordCommand(string Email) : ICommand<Result<ForgotPasswordResult>>;