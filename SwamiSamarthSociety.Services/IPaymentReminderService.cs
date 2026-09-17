namespace SwamiSamarthSociety.Services
{
    public class ReminderBatchResult
    {
        public int TotalActiveMembers { get; set; }
        public int SkippedNoMobile { get; set; }
        public int Sent { get; set; }
        public int Failed { get; set; }
    }

    public interface IPaymentReminderService
    {
        // Builds and sends this month's payment reminder to every active member with a mobile
        // number on file (loan installment amount if they have an active loan, else just their
        // share amount), and logs every attempt to SmsLog.
        Task<ReminderBatchResult> SendMonthlyRemindersAsync(CancellationToken ct = default);
    }
}
