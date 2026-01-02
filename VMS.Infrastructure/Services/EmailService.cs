using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VMS.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _smtpHost = configuration["SmtpSettings:Host"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(configuration["SmtpSettings:Port"] ?? "587");
        _smtpUsername = configuration["SmtpSettings:Username"] ?? string.Empty;
        _smtpPassword = configuration["SmtpSettings:Password"] ?? string.Empty;
        _fromEmail = configuration["SmtpSettings:FromEmail"] ?? "noreply@vms.com";
        _fromName = configuration["SmtpSettings:FromName"] ?? "VMS System";
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        try
        {
            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            message.To.Add(toEmail);

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            // In production, you might want to throw or handle this differently
        }
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string firstName, string temporaryPassword)
    {
        var subject = "Welcome to VMS - Account Created";
        var body = $@"
            <html>
            <body>
                <h2>Welcome to VMS, {firstName}!</h2>
                <p>Your account has been created successfully.</p>
                <p><strong>Temporary Password:</strong> {temporaryPassword}</p>
                <p>Please log in and change your password as soon as possible.</p>
                <p>Best regards,<br/>VMS Team</p>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendApprovalRequestNotificationAsync(string toEmail, string approverName, string guestName, string requestType)
    {
        var subject = $"Approval Request - {requestType} for {guestName}";
        var body = $@"
            <html>
            <body>
                <h2>New Approval Request</h2>
                <p>Hello {approverName},</p>
                <p>A new {requestType.ToLower()} request has been submitted for <strong>{guestName}</strong>.</p>
                <p>Please review and approve or reject this request in your dashboard.</p>
                <p>Best regards,<br/>VMS Team</p>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendApprovalConfirmationAsync(string toEmail, string guestName, bool isApproved, string? comments = null)
    {
        var status = isApproved ? "Approved" : "Rejected";
        var subject = $"Guest Request {status} - {guestName}";
        var body = $@"
            <html>
            <body>
                <h2>Request {status}</h2>
                <p>The request for <strong>{guestName}</strong> has been <strong>{status.ToLower()}</strong>.</p>
                {(string.IsNullOrWhiteSpace(comments) ? "" : $"<p><strong>Comments:</strong> {comments}</p>")}
                <p>Best regards,<br/>VMS Team</p>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendInvitationConfirmationAsync(string toEmail, string guestName, DateTime arrivalDate)
    {
        var subject = $"Invitation Confirmation - {guestName}";
        var body = $@"
            <html>
            <body>
                <h2>Invitation Confirmed</h2>
                <p>Your invitation for <strong>{guestName}</strong> has been confirmed.</p>
                <p><strong>Expected Arrival:</strong> {arrivalDate:dd MMM yyyy HH:mm}</p>
                <p>Best regards,<br/>VMS Team</p>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendInvitationCancellationNotificationAsync(string toEmail, string guestName, string invitationNo)
    {
        var subject = $"Invitation Cancelled - {invitationNo}";
        var body = $@"
            <html>
            <body>
                <h2>Invitation Cancelled</h2>
                <p>Your invitation for <strong>{guestName}</strong> has been cancelled.</p>
                <p><strong>Invitation Number:</strong> {invitationNo}</p>
                <p>If you have any questions, please contact the host or administrator.</p>
                <p>Best regards,<br/>VMS Team</p>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body);
    }
}













