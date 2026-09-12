namespace Account.Domain.Models;

public record OtpSessionCreateParams(string CodeHash, Guid UserId, Guid CorrelationId);