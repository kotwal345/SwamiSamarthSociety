namespace SwamiSamarthSociety.Services
{
    // One row of the monthly collection sheet: a member's share due/paid for the cycle,
    // plus (if they have an active loan) that loan's installment for the same cycle.
    public class MonthlyCycleRow
    {
        public int MemberId { get; set; }
        public string MemberCode { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? FullNameMarathi { get; set; }

        public decimal ShareExpected { get; set; }
        public decimal SharePaid { get; set; }
        public string ShareStatus { get; set; } = "Pending";

        // Cumulative sum of this member's share amount across every cycle up to and including this one.
        public decimal TotalShare { get; set; }

        public bool HasActiveLoan { get; set; }
        public int? LoanId { get; set; }
        public string? LoanNumber { get; set; }
        public decimal? OpeningPrincipal { get; set; }
        public decimal? PrincipalDue { get; set; }
        public decimal? InterestDue { get; set; }
        public decimal? LoanPaidAmount { get; set; }
        public string? LoanStatus { get; set; }
        public decimal? RemainingBalance { get; set; }

        // Share + principal + interest due this cycle, matching the club's monthly "एकूण रक्कम" column.
        public decimal TotalAmount => ShareExpected + (PrincipalDue ?? 0) + (InterestDue ?? 0);

        // Share + loan amount actually collected this cycle -- what the member has physically paid so far.
        public decimal TotalPaid => SharePaid + (LoanPaidAmount ?? 0);
    }
}
