using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.CreateApiKey;

public record CreateApiKeyCommand(string Email) : ICommand<Result<string>>;