namespace Account.Infrastructure.Configuration;

public class AppUrlOptions
{
    public string PublicBaseUrl { get; set; } = string.Empty;
    public string VerifyEmailPath { get; set; } = "/api/account/verify-email";
}