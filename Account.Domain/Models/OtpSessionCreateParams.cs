namespace Account.Domain.Models;

public record OtpSessionCreateParams(string CodeHash, string UserId);