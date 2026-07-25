using ATS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ATS.Infrastructure.Email;

// SMTP-based implementation. Swap in SendGrid/Azure Communication Services by
// implementing IEmailService and changing the DI registration below.
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var host = _config["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogInformation("Email (SMTP not configured, logging only) → {To}: {Subject}", to, subject);
            return;
        }

        using var client = new System.Net.Mail.SmtpClient(host, int.Parse(_config["Smtp:Port"] ?? "587"))
        {
            Credentials = new System.Net.NetworkCredential(_config["Smtp:Username"], _config["Smtp:Password"]),
            EnableSsl = true
        };

        using var message = new System.Net.Mail.MailMessage(
            _config["Smtp:FromAddress"] ?? "noreply@ats.local", to, subject, htmlBody)
        {
            IsBodyHtml = true
        };

        await client.SendMailAsync(message, ct);
    }
}
