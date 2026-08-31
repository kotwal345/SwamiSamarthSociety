using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwamiSamarthSociety.Data.Entities
{

    public class MonthlyCycle
    {
        public int MonthlyCycleId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "Open";
        public decimal ExpectedShareAmount { get; set; }
        public decimal CollectedShareAmount { get; set; }
        public decimal PendingShareAmount { get; set; }
        public decimal TotalPrincipalCollected { get; set; }
        public decimal TotalInterestCollected { get; set; }
        public decimal TotalLoanDistributed { get; set; }
        public decimal TotalPenaltyCollected { get; set; }
        public decimal OpeningBankBalance { get; set; }
        public decimal? ClosingBankBalance { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedDate { get; set; }

        public ICollection<MemberSharePayment> SharePayments { get; set; } = new List<MemberSharePayment>();
        public ICollection<LoanInstallment> LoanInstallments { get; set; } = new List<LoanInstallment>();
    }
}