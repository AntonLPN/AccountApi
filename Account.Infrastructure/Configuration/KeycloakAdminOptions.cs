namespace Account.Infrastructure.Configuration;

public class KeycloakAdminOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public bool? EmailVerifiedByDefault { get; set; }
}