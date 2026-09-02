using System.ComponentModel.DataAnnotations;

namespace Account.Infrastructure.Configuration;

public class KeycloakAdminOptions
{
    [Required] public string BaseUrl { get; set; } = string.Empty;
    [Required] public string Realm { get; set; } = string.Empty;
    [Required] public string ClientId { get; set; } = string.Empty;
    [Required] public string ClientSecret { get; set; } = string.Empty;
    public bool? EmailVerifiedByDefault { get; set; }
}