using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwamiSamarthSociety.Data.Entities
{
    public class MemberExitSettlement : ITenantScoped
    {
        public int MemberExitSettlementId { get; set; }
        public int SocietyId { get; set; }
        public int MemberId { get; set; }
        public Member Member { get; set; } = null!;
        public DateTime ExitDate { get; set; }
        public decimal AccumulatedShare { get; set; }
        public int MembershipMonths { get; set; }
        public int MonthsDeducted { get; set; }
        public decimal MaintenanceDeduction { get; set; }
        public decimal FinalSettlementAmount { get; set; }
        public string Status { get; set; } = "PendingApproval";
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
    }
}
