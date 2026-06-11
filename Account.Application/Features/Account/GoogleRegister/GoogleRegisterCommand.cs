using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.GoogleRegister;

public record GoogleRegisterCommand(string GoogleToken, string ReferrerCode) : ICommand<Result<GoogleRegisterResult>>;