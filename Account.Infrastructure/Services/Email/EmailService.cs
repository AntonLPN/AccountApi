using Account.Domain.DTOs;
using Account.Domain.Interfaces;
using Account.Infrastructure.Configuration;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using Polly;

namespace Account.Infrastructure.Services.Email;

public class EmailService(IConfiguration configuration, ILogger<EmailService> logger) : IEmail
{
    private readonly EmailConfig _emailConfig =
        configuration.GetSection("Messaging:Email").Get<EmailConfig>()
        ?? throw new InvalidOperationException("Email configuration is missing.");

    private readonly IAsyncPolicy<bool> _emailRetryPolicy = PollyPolicies.GetEmailRetryWithCircuitBreaker();

    private void ValidateConfiguration()
    {
        ArgumentException.ThrowIfNullOrEmpty(_emailConfig.HostName, nameof(_emailConfig.HostName));
        ArgumentException.ThrowIfNullOrEmpty(_emailConfig.Email, nameof(_emailConfig.Email));
        ArgumentException.ThrowIfNullOrEmpty(_emailConfig.Password, nameof(_emailConfig.Password));
    }

    private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody,
        CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();
        var bodyBuilder = new BodyBuilder()
        {
            HtmlBody = htmlBody
        };
        MimeMessage msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_emailConfig.OwnerName, _emailConfig.Email!));
        msg.To.Add(new MailboxAddress(toEmail, toEmail));
        msg.Subject = subject;
        msg.Body = bodyBuilder.ToMessageBody();
        try
        {
            return await SendMessageSmtp(msg, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occurred while sending email");
            throw;
        }
    }

    public async Task<bool> SendWelcomeEmail(string toEmail, CancellationToken cancellationToken = default)
    {
        var htmlTemplate = GetEmailTemplateAsync("WelcomeTemplate.html");
        ArgumentException.ThrowIfNullOrEmpty(htmlTemplate);
        return await _emailRetryPolicy.ExecuteAsync(async () => await SendEmailAsync(
            toEmail,
            "Welcome to " + _emailConfig.OwnerName,
            htmlTemplate,
            cancellationToken));
    }

    public async Task<bool> SendNewDeviceLoginEmail(SuspiciousDeviceDto suspiciousDeviceDto,
        CancellationToken cancellationToken = default)
    {
        var htmlTemplate = GetEmailTemplateAsync("SuspiciousLoginTemplate.html");
        var body = htmlTemplate
            .Replace("{{DEVICE_NAME}}", suspiciousDeviceDto.DeviceName)
            .Replace("{{IP_ADDRESS}}", suspiciousDeviceDto.IpAddress)
            .Replace("{{DATE_TIME}}", suspiciousDeviceDto.LoginTime.ToString("dd.MM.yyyy, HH:mm 'UTC'"))
            .Replace("{{USER_AGENT}}", suspiciousDeviceDto.UserAgent);
        ArgumentException.ThrowIfNullOrEmpty(htmlTemplate);
        return await _emailRetryPolicy.ExecuteAsync(async () => await SendEmailAsync(
            suspiciousDeviceDto.ToEmail,
            "New Device Login — " + _emailConfig.OwnerName,
            body,
            cancellationToken));
    }

    public async Task<bool> SendLogoutNotificationEmail(LogoutNotificationDto logoutNotificationDto,
        CancellationToken cancellationToken = default)
    {
        var htmlTemplate = GetEmailTemplateAsync("LogoutTemplate.html");
        var body = htmlTemplate
            .Replace("{{IP_ADDRESS}}", logoutNotificationDto.IpAddress)
            .Replace("{{DATE_TIME}}", logoutNotificationDto.LogoutTime.ToString("dd.MM.yyyy, HH:mm 'UTC'"))
            .Replace("{{USER_AGENT}}", logoutNotificationDto.UserAgent);
        ArgumentException.ThrowIfNullOrEmpty(htmlTemplate);
        return await _emailRetryPolicy.ExecuteAsync(async () => await SendEmailAsync(
            logoutNotificationDto.ToEmail,
            "You have been logged out — " + _emailConfig.OwnerName,
            body,
            cancellationToken));
    }

    private async Task<bool> SendMessageSmtp(MimeMessage message, CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();
        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_emailConfig.HostName!, _emailConfig.Port,
                MailKit.Security.SecureSocketOptions.StartTls, cancellationToken);
            await client.AuthenticateAsync(_emailConfig.Email!, _emailConfig.Password!, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "SMTP error");
            throw; // Polly will catch this and handle retries
        }
    }

    private static string GetEmailTemplateAsync(string templateName)
    {
        var assembly = typeof(EmailService).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .SingleOrDefault(n => n.EndsWith(templateName, StringComparison.OrdinalIgnoreCase));

        if (resourceName == null)
        {
            var available = string.Join(", ", assembly.GetManifestResourceNames());
            throw new InvalidOperationException($"Email template resource not found. Available resources: {available}");
        }

        using var stream = assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"Email template resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}