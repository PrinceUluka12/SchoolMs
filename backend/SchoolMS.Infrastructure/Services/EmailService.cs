using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using SchoolMS.Core.Interfaces;

namespace SchoolMS.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        var smtpHost = _config["Email:SmtpHost"];
        var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
        var smtpUser = _config["Email:SmtpUser"];
        var smtpPass = _config["Email:SmtpPass"];
        var fromEmail = _config["Email:FromEmail"] ?? "noreply@schoolms.com";
        var fromName = _config["Email:FromName"] ?? "SchoolMS";

        if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser))
        {
            // Dev mode: log instead of sending
            Console.WriteLine($"[EMAIL] To: {toEmail} | Subject: {subject}");
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(smtpUser, smtpPass);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    public async Task SendAbsenceAlertAsync(
        string guardianEmail, string guardianName, string studentName, DateTime date)
    {
        var html = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <div style="background: #1F3864; color: white; padding: 24px; border-radius: 8px 8px 0 0;">
                    <h2 style="margin: 0;">Absence Notification</h2>
                    <p style="margin: 4px 0 0; opacity: 0.8;">SchoolMS Student Alert</p>
                </div>
                <div style="background: #f9f9f9; padding: 24px; border: 1px solid #ddd; border-radius: 0 0 8px 8px;">
                    <p>Dear {guardianName},</p>
                    <p>This is to inform you that <strong>{studentName}</strong> was marked
                    <strong style="color: #DC2626;">ABSENT</strong> on
                    <strong>{date:dddd, MMMM d, yyyy}</strong>.</p>
                    <p>If this is an error or you have already notified the school, please disregard this message.
                    Otherwise, please contact the school as soon as possible.</p>
                    <hr style="border: none; border-top: 1px solid #ddd; margin: 16px 0;" />
                    <p style="color: #999; font-size: 12px;">This is an automated message from SchoolMS.
                    Please do not reply to this email.</p>
                </div>
            </div>
            """;

        await SendAsync(guardianEmail, guardianName, $"Absence Alert: {studentName} — {date:MMM d, yyyy}", html);
    }

    public async Task SendFeeReminderAsync(
        string guardianEmail, string guardianName, string studentName, decimal balance, DateTime dueDate)
    {
        var html = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <div style="background: #1F3864; color: white; padding: 24px; border-radius: 8px 8px 0 0;">
                    <h2 style="margin: 0;">Fee Payment Reminder</h2>
                    <p style="margin: 4px 0 0; opacity: 0.8;">SchoolMS Finance</p>
                </div>
                <div style="background: #f9f9f9; padding: 24px; border: 1px solid #ddd; border-radius: 0 0 8px 8px;">
                    <p>Dear {guardianName},</p>
                    <p>This is a reminder that there is an outstanding fee balance for
                    <strong>{studentName}</strong>.</p>
                    <div style="background: #FEF3C7; border: 1px solid #F59E0B; border-radius: 8px; padding: 16px; margin: 16px 0;">
                        <p style="margin: 0;"><strong>Outstanding Balance:</strong>
                        <span style="color: #DC2626; font-size: 20px;"> {balance:C}</span></p>
                        <p style="margin: 8px 0 0;"><strong>Due Date:</strong> {dueDate:MMMM d, yyyy}</p>
                    </div>
                    <p>Please make payment before the due date to avoid late fees.
                    Contact the finance office for payment options.</p>
                    <hr style="border: none; border-top: 1px solid #ddd; margin: 16px 0;" />
                    <p style="color: #999; font-size: 12px;">This is an automated message from SchoolMS.</p>
                </div>
            </div>
            """;

        await SendAsync(guardianEmail, guardianName,
            $"Fee Reminder: {studentName} — Balance {balance:C}", html);
    }

    public async Task SendReceiptAsync(
        string guardianEmail, string guardianName, string studentName,
        string receiptNumber, decimal amount)
    {
        var html = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <div style="background: #1F3864; color: white; padding: 24px; border-radius: 8px 8px 0 0;">
                    <h2 style="margin: 0;">Payment Receipt</h2>
                    <p style="margin: 4px 0 0; opacity: 0.8;">SchoolMS Finance</p>
                </div>
                <div style="background: #f9f9f9; padding: 24px; border: 1px solid #ddd; border-radius: 0 0 8px 8px;">
                    <p>Dear {guardianName},</p>
                    <p>We have received your payment for <strong>{studentName}</strong>. Thank you.</p>
                    <div style="background: #D1FAE5; border: 1px solid #10B981; border-radius: 8px; padding: 16px; margin: 16px 0;">
                        <p style="margin: 0;"><strong>Receipt Number:</strong> {receiptNumber}</p>
                        <p style="margin: 8px 0 0;"><strong>Amount Paid:</strong>
                        <span style="color: #059669; font-size: 20px;"> {amount:C}</span></p>
                        <p style="margin: 8px 0 0;"><strong>Date:</strong> {DateTime.UtcNow:MMMM d, yyyy}</p>
                    </div>
                    <p>Please keep this receipt for your records.</p>
                    <hr style="border: none; border-top: 1px solid #ddd; margin: 16px 0;" />
                    <p style="color: #999; font-size: 12px;">This is an automated message from SchoolMS.</p>
                </div>
            </div>
            """;

        await SendAsync(guardianEmail, guardianName,
            $"Payment Receipt #{receiptNumber} — {studentName}", html);
    }
}