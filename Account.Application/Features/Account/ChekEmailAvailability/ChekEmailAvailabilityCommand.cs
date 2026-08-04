using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.ChekEmailAvailability;

public record  ChekEmailAvailabilityCommand(string Email) : ICommand<Result<bool>>;