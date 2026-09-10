using Account.Domain.Entities;
using Account.Domain.Repositories;
using Account.Domain.Specifications;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.Setup2Fa;

public class Setup2FaHandler(
    ILogger<Setup2FaHandler> logger,
    IRepository<AppUser> userRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<Setup2FaCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(Setup2FaCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Email))
            return Result<bool>.Invalid(new ValidationError("Email", "Email cannot be null or empty"));

        var normalizedEmail = Email.Create(request.Email);
        try
        {
            var user = await userRepository.FirstOrDefaultAsync(new UserByEmailSpec(request.Email), cancellationToken);
            if (user is null)
                return Result<bool>.NotFound("User not found");
            if (!user.EmailConfirmed)
                return Result<bool>.Conflict("Email not confirmed for enable or disable 2FA");
            
            await using var tx = await unitOfWork.BeginTransactionAsync(cancellationToken);
            user.SetTwoFactor(request.IsEnable);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred while setting up 2FA for user with email {Email}",
                MaskedEmail.Create(normalizedEmail));
            throw;
        }
    }
}