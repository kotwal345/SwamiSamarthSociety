using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwamiSamarthSociety.Data.Entities
{
    public class Member
    {
        public int MemberId { get; set; }
        public string MemberCode { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? FullNameMarathi { get; set; }
        public string? MobileNumber { get; set; }
        public string? Address { get; set; }
        public DateTime JoiningDate { get; set; }
        public DateTime? LeavingDate { get; set; }
        public string Status { get; set; } = "Active"; // Active, Inactive, Left
        public decimal MonthlyShareAmount { get; set; } = 500m;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedDate { get; set; }

        public ICollection<MemberSharePayment> SharePayments { get; set; } = new List<MemberSharePayment>();
        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
        public ICollection<LoanGuarantor> GuarantorFor { get; set; } = new List<LoanGuarantor>();
    }
}