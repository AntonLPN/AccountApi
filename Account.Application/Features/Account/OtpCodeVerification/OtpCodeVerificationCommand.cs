using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.OtpCodeVerification;

public record OtpCodeVerificationCommand(string Email, string OtpCode) : ICommand<Result<OtpConfirmationResult>>;