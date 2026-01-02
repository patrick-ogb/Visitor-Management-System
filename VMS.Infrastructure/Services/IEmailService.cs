namespace VMS.Infrastructure.Services;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail, string firstName, string temporaryPassword);
    Task SendApprovalRequestNotificationAsync(string toEmail, string approverName, string guestName, string requestType);
    Task SendApprovalConfirmationAsync(string toEmail, string guestName, bool isApproved, string? comments = null);
    Task SendInvitationConfirmationAsync(string toEmail, string guestName, DateTime arrivalDate);
    Task SendInvitationCancellationNotificationAsync(string toEmail, string guestName, string invitationNo);
}













