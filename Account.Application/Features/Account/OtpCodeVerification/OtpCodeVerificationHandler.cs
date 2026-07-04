using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.OtpCodeVerification;

public class OtpCodeVerificationHandler(ILogger<OtpCodeVerificationHandler> logger) : ICommandHandler<OtpCodeVerificationCommand, Result<OtpConfirmationResult>>
{
    public Task<Result<OtpConfirmationResult>> Handle(OtpCodeVerificationCommand request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(request.OtpCode, nameof(request.OtpCode));
        
        
        
        throw new NotImplementedException();
    }
}