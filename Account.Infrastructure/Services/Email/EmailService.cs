using System.Net;
using Account.Domain.DTOs;
using Account.Domain.Interfaces;
using Account.Infrastructure.Configuration;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Account.Infrastructure.Services.Email;

public class EmailService(IConfiguration configuration, ILogger<EmailService> logger) : IEmail
{
    private readonly EmailConfig _emailConfig =
        configuration.GetSection("Messaging:Email").Get<EmailConfig>()
        ?? throw new InvalidOperationException("Email configuration is missing.");

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
            await SendMessageSmtp(msg, cancellationToken);
            return true;
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
        ArgumentException.ThrowIfNullOrEmpty(htmlTemplate, nameof(htmlTemplate));
        return await SendEmailAsync(toEmail, "Welcome to " + _emailConfig.OwnerName, htmlTemplate, cancellationToken);
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
        ArgumentException.ThrowIfNullOrEmpty(htmlTemplate, nameof(htmlTemplate));
        return await SendEmailAsync(suspiciousDeviceDto.ToEmail, "New Device Login — " + _emailConfig.OwnerName, body,
            cancellationToken);
    }

    private async Task<bool> SendMessageSmtp(MimeMessage message, CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();
        using var client = new SmtpClient();
        await client.ConnectAsync(_emailConfig.HostName, _emailConfig.Port,
            MailKit.Security.SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(_emailConfig.Email, _emailConfig.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        return true;
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