namespace Account.Domain.Interfaces;

public interface IEmail
{
    Task<bool> SendEmail(string fromEmail, string toEmail, string htmlSubject,
        CancellationToken cancellationToken = default);
}