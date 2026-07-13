namespace Account.Infrastructure.Configuration;

public class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    public SchemesOptions Schemes { get; set; } = new();
    public PreAuthOptions PreAuth { get; set; } = new();
}

public class SchemesOptions
{
    public BearerOptions Bearer { get; set; } = new();
}

public class BearerOptions
{
    public string Authority { get; set; } = string.Empty;
    public string MetadataAddress { get; set; } = string.Empty;
    public string ValidAudience { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; }
}

public class PreAuthOptions
{
    public string SigningKey { get; set; } = string.Empty;
}