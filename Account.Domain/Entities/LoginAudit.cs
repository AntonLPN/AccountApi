namespace Account.Domain.Entities;

public class LoginAudit
{
    public long Id { get; set; }
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsSuspicious { get; set; }
    public DateTime LoggedInAt { get; set; }
}