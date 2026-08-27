using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Specifications;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.SendEmailVerification;

public class SendEmailVerificationHandler(
    ILogger<SendEmailVerificationHandler> logger,
    IRepository<AppUser> userRepository,
    IEmail emailService) : ICommandHandler<SendEmailVerificationCommand, Result<string>>
{
    public async Task<Result<string>> Handle(SendEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(request.Email, nameof(request.Email));
        try
        {
            var user = await userRepository.FirstOrDefaultAsync(
                new UserByEmailSpec(request.Email),
                cancellationToken);
            if (user is null)
                return Result<string>.NotFound("User not found");
            var token = Guid.NewGuid().ToString("N");
            var verificationUrl = request.BaseUrl + $"?token={token}";
            var isSend = await emailService.SendVerificationEmailAsync(request.Email, verificationUrl, cancellationToken);
            return !isSend ? Result<string>.Error("Failed to send email") : Result<string>.Success(token);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        throw new NotImplementedException();
    }
}