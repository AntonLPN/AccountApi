using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.Setup2Fa;

public record Setup2FaCommand(string Email, bool IsEnable) : ICommand<Result>;