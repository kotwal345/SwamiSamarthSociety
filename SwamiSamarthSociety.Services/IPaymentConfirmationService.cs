namespace SwamiSamarthSociety.Services
{
    public interface IPaymentConfirmationService
    {
        // Sends a "payment received" WhatsApp notification to the member (if they have a mobile
        // number on file) and logs the attempt to SmsLog, mirroring how reminders are logged.
        // No-op if amountPaid is 0 or less -- nothing to confirm.
        Task NotifyAsync(int memberId, decimal amountPaid, DateTime paymentDate, CancellationToken ct = default);
    }
}
