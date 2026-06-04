using Account.Domain.Interfaces;

namespace Account.Infrastructure.Services.Email;

public class EmailService : IEmail
{
    public Task<bool> SendEmail(string fromEmail, string toEmail, string htmlSubject, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}