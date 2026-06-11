using Account.Application.Features.Account.Models;
using Account.Domain.Models;

namespace Account.Application.Features.Account.Authentication;

public class AuthenticationResult
{
    public TokenResponse? Token { get; set; }
}