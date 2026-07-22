using Account.Application.Features.Account.ChangePassword;
using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.ForgotPassword;

public record ForgotPasswordCommand(string Email) : ICommand<Result<ForgotPasswordResult>>;