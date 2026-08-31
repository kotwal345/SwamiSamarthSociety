using System.ComponentModel.DataAnnotations;
using SwamiSamarthSociety.Data.Entities;

namespace SwamiSamarthSociety.Web.Models
{
    public class LoanCreateViewModel
    {
        [Required(ErrorMessage = "Please select a borrower.")]
        [Display(Name = "Borrower")]
        public int MemberId { get; set; }

        [Range(1, double.MaxValue, ErrorMessage = "Loan amount must be greater than zero.")]
        [Display(Name = "Loan Amount")]
        public decimal OriginalLoanAmount { get; set; }

        [Display(Name = "Loan Date")]
        public DateTime LoanDate { get; set; } = DateTime.Today;

        [Display(Name = "Interest Rate (% per month)")]
        public decimal InterestRate { get; set; } = 2m;

        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }

        public List<int> GuarantorMemberIds { get; set; } = new();

        // Populated by the controller for redisplay; not bound from the form.
        public List<Member> BorrowerCandidates { get; set; } = new();
        public List<Member> GuarantorCandidates { get; set; } = new();
        public int GuarantorsRequired { get; set; } = 2;
    }
}
