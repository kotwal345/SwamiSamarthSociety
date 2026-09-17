using System;

namespace SwamiSamarthSociety.Data.Entities
{
    public class SmsLog
    {
        public int SmsLogId { get; set; }
        public int MemberId { get; set; }
        public Member Member { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string ReminderType { get; set; } = null!; // "Loan" or "Share"
        public DateTime SentDate { get; set; } = DateTime.UtcNow;
        public bool Success { get; set; }
        public string? ProviderResponse { get; set; }
    }
}
