namespace Account.Domain.DTOs;

public class CreateLogoutCreateParams
{
    public Guid UserId { get; set; } 
    public string Email { get; set; } = "";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime LoggedOutAt { get; set; }
}