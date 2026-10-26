using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.SendOtpCode;

public record GenerateOtpCodeCommand (string Email) : ICommand<Result<GenerateOtpCodeResult>>;