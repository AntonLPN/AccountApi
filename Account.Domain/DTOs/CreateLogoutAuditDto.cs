namespace Account.Domain.DTOs;

public class CreateLogoutAuditDto
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime LoggedOutAt { get; set; }
}