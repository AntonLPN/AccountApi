using Account.Domain.Entities;
using Account.Domain.Specifications;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.ChekEmailAvailability;

public class ChekEmailAvailabilityHandler(
    ILogger<ChekEmailAvailabilityHandler> logger,
    IRepository<AppUser> userRepository)
    : ICommandHandler<ChekEmailAvailabilityCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ChekEmailAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = Email.Create(request.Email);
        try
        {
            var user = await userRepository.FirstOrDefaultAsync(new UserByEmailSpec(normalizedEmail), cancellationToken);
            return Result<bool>.Success(user is null);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred while handling ChekEmailAvailabilityCommand for email {Email}",
                MaskedEmail.Create(normalizedEmail));
            throw;
        }
    }
}