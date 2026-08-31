namespace SwamiSamarthSociety.Services
{
    // One row of the monthly collection sheet: a member's share due/paid for the cycle,
    // plus (if they have an active loan) that loan's installment for the same cycle.
    public class MonthlyCycleRow
    {
        public int MemberId { get; set; }
        public string MemberCode { get; set; } = null!;
        public string FullName { get; set; } = null!;

        public decimal ShareExpected { get; set; }
        public decimal SharePaid { get; set; }
        public string ShareStatus { get; set; } = "Pending";

        public bool HasActiveLoan { get; set; }
        public int? LoanId { get; set; }
        public string? LoanNumber { get; set; }
        public decimal? OpeningPrincipal { get; set; }
        public decimal? PrincipalDue { get; set; }
        public decimal? InterestDue { get; set; }
        public decimal? LoanPaidAmount { get; set; }
        public string? LoanStatus { get; set; }
        public decimal? RemainingBalance { get; set; }
    }
}
