using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.SendEmailVerification;

public record SendEmailVerificationCommand(string Email) : ICommand<Result<string>>;