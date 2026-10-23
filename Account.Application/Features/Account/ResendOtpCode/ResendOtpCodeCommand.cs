using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.ResendOtpCode;

public record ResendOtpCodeCommand(string Email) : ICommand<Result<bool>>;
