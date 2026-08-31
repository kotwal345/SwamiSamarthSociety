using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace SwamiSamarthSociety.Data.Entities
{
    public class MemberSharePayment
    {
        public int SharePaymentId { get; set; }
        public int MemberId { get; set; }
        public Member Member { get; set; } = null!;
        public int MonthlyCycleId { get; set; }
        public MonthlyCycle MonthlyCycle { get; set; } = null!;
        public decimal ExpectedAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal PenaltyAmount { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Paid, PartiallyPaid, Late, PenaltyApplied
        public string? ReceiptNumber { get; set; }
        public string? Remarks { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
