using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.IsEmailVerified;

public record IsEmailVerifiedCommand(string Email) : ICommand<Result<IsEmailVerifiedResult>>;
