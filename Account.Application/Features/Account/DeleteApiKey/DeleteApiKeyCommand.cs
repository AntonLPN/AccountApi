using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.DeleteApiKey;

public sealed record DeleteApiKeyCommand(string Email, string ApiKey) : ICommand<Result<bool>>;
