namespace Account.Infrastructure.Configuration;

public class EmailConfig
{
    public string? OwnerName { get; set; }
    public string? Email { get; set; }
    public string? HostName { get; set; }
    public string? Password { get; set; }
    public int Port { get; set; } = 587;
}