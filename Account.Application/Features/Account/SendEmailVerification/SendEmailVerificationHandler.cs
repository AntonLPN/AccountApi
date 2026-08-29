using System.Web;
using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Specifications;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.SendEmailVerification;

public class SendEmailVerificationHandler(
    ILogger<SendEmailVerificationHandler> logger,
    IRepository<AppUser> userRepository,
    IEmail emailService,
    IDataCache dataCache) : ICommandHandler<SendEmailVerificationCommand, Result<string>>
{
    private const string PREFIX = "email_verification_";

    public async Task<Result<string>> Handle(SendEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(request.Email, nameof(request.Email));
        var normalizedEmail = Email.Create(request.Email);
        try
        {
            var user = await userRepository.FirstOrDefaultAsync(
                new UserByEmailSpec(normalizedEmail),
                cancellationToken);
            if (user is null)
                return Result<string>.NotFound("User not found");
            var token = Guid.NewGuid().ToString("N");
            await dataCache.SetStringAsync(PREFIX + token, normalizedEmail, TimeSpan.FromMinutes(10));
            var isSend = await emailService.SendVerificationEmailAsync(request.Email, token, cancellationToken);
            return !isSend ? Result<string>.Error("Failed to send email") : Result<string>.Success(token);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occured while sending email verification");
            throw;
        }
    }
}