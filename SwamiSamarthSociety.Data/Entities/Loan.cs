using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwamiSamarthSociety.Data.Entities
{
    public class Loan : ITenantScoped
    {
        public int LoanId { get; set; }
        public int SocietyId { get; set; }
        public string LoanNumber { get; set; } = null!;
        public int MemberId { get; set; }
        public Member Member { get; set; } = null!;
        public decimal OriginalLoanAmount { get; set; }
        public DateTime LoanDate { get; set; }
        public decimal InterestRate { get; set; } = 2m;
        public decimal? InstallmentPercentage { get; set; }
        public int? NumberOfInstallments { get; set; }
        public decimal? InstallmentAmount { get; set; }
        public decimal TotalPrincipalPaid { get; set; }
        public decimal TotalInterestPaid { get; set; }
        public decimal OutstandingPrincipal { get; set; }
        public string Status { get; set; } = "Active"; // Active, Completed, Defaulted
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? DisbursementDate { get; set; }
        public bool ChequeSubmitted { get; set; }
        public DateTime? ChequeSubmissionDate { get; set; }
        public string? Remarks { get; set; }

        public ICollection<LoanGuarantor> Guarantors { get; set; } = new List<LoanGuarantor>();
        public ICollection<LoanInstallment> Installments { get; set; } = new List<LoanInstallment>();
    }

    public class LoanGuarantor : ITenantScoped
    {
        public int LoanGuarantorId { get; set; }
        public int SocietyId { get; set; }
        public int LoanId { get; set; }
        public Loan Loan { get; set; } = null!;
        public int GuarantorMemberId { get; set; }
        public Member GuarantorMember { get; set; } = null!;
        public string Status { get; set; } = "Active";
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class LoanInstallment : ITenantScoped
    {
        public int LoanInstallmentId { get; set; }
        public int SocietyId { get; set; }
        public int LoanId { get; set; }
        public Loan Loan { get; set; } = null!;
        public int MonthlyCycleId { get; set; }
        public MonthlyCycle MonthlyCycle { get; set; } = null!;
        public int InstallmentNumber { get; set; }
        public DateTime DueDate { get; set; }
        public decimal OpeningPrincipal { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal ShareAmount { get; set; }
        public decimal PenaltyAmount { get; set; }
        // Principal left unpaid from the previous installment, demanded back on top of this
        // month's regular PrincipalAmount instead of silently riding along inside OutstandingPrincipal.
        public decimal ArrearsAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal ClosingPrincipal { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string Status { get; set; } = "Pending";

        public ICollection<LoanPayment> Payments { get; set; } = new List<LoanPayment>();
    }

    public class LoanPayment : ITenantScoped
    {
        public int LoanPaymentId { get; set; }
        public int SocietyId { get; set; }
        public int LoanInstallmentId { get; set; }
        public LoanInstallment LoanInstallment { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? ReceiptNumber { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
