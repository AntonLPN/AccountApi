namespace Account.Domain.Interfaces;

public interface IEmail
{
    Task<bool> SendEmail(string toEmail, string htmlTemplateName,
        CancellationToken cancellationToken = default);
}