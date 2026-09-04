using EventParking.Business.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace EventParking.Business.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(
        string email,
        string fullName,
        string token)
    {
        var frontendUrl =
            _configuration["Frontend:BaseUrl"]
            ?? "http://localhost:4200";

        var link =
            $"{frontendUrl}/verify-email?token={Uri.EscapeDataString(token)}";

        var body =
            $"Hello {fullName},\n\n" +
            $"Verify your email using this link:\n{link}";

        await SendAsync(
            email,
            "Verify Your Email",
            body,
            link);
    }

    public async Task SendPasswordResetEmailAsync(
        string email,
        string fullName,
        string token)
    {
        var frontendUrl =
            _configuration["Frontend:BaseUrl"]
            ?? "http://localhost:4200";

        var link =
            $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(token)}";

        var body =
            $"Hello {fullName},\n\n" +
            $"Reset your password using this link:\n{link}";

        await SendAsync(
            email,
            "Reset Your Password",
            body,
            link);
    }

    private async Task SendAsync(
        string recipient,
        string subject,
        string body,
        string developmentLink)
    {
        var enableSending =
            bool.TryParse(
                _configuration["Email:EnableSending"],
                out var enabled)
            && enabled;

        // Development mode:
        // Real email sending disabled.
        // Verification/reset link is written to Visual Studio output.
        if (!enableSending)
        {
            _logger.LogInformation(
                "Development email for {Recipient}: {Link}",
                recipient,
                developmentLink);

            return;
        }

        var host =
            _configuration["Email:SmtpHost"];

        var username =
            _configuration["Email:Username"];

        var password =
            _configuration["Email:Password"];

        var fromEmail =
            _configuration["Email:FromEmail"];

        var fromName =
            _configuration["Email:FromName"]
            ?? "Event & Parking Reservation System";

        var port =
            int.TryParse(
                _configuration["Email:SmtpPort"],
                out var configuredPort)
                ? configuredPort
                : 587;

        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException(
                "Email SMTP host is not configured.");
        }

        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException(
                "Email sender address is not configured.");
        }

        if (!string.IsNullOrWhiteSpace(username) &&
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Email SMTP password is not configured.");
        }

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                fromName,
                fromEmail));

        message.To.Add(
            MailboxAddress.Parse(recipient));

        message.Subject = subject;

        message.Body =
            new TextPart("plain")
            {
                Text = body
            };

        using var client = new SmtpClient();

        await client.ConnectAsync(
            host,
            port,
            SecureSocketOptions.StartTls);

        if (!string.IsNullOrWhiteSpace(username) &&
            !string.IsNullOrWhiteSpace(password))
        {
            await client.AuthenticateAsync(
                username,
                password);
        }

        await client.SendAsync(message);

        await client.DisconnectAsync(true);
    }
}