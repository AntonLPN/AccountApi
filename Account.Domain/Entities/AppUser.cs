
using Microsoft.AspNetCore.Identity;

namespace Account.Domain.Entities;

public class AppUser:IdentityUser
{
    public ICollection<ApiKey> ApiKeys { get; set; } = [];
}