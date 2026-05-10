namespace SchoolMS.Core.Interfaces;

public interface IEmailService
{
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody);
    Task SendAbsenceAlertAsync(string guardianEmail, string guardianName, string studentName, DateTime date);
    Task SendFeeReminderAsync(string guardianEmail, string guardianName, string studentName, decimal balance, DateTime dueDate);
    Task SendReceiptAsync(string guardianEmail, string guardianName, string studentName, string receiptNumber, decimal amount);
}