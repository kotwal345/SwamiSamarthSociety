using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwamiSamarthSociety.Data.Entities
{
    public class LoanInstallmentRule : ITenantScoped
    {
        public int LoanInstallmentRuleId { get; set; }
        public int SocietyId { get; set; }
        public decimal MinimumAmount { get; set; }
        public decimal MaximumAmount { get; set; }
        public decimal? InstallmentPercentage { get; set; }
        public int? NumberOfInstallments { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // Per-society key/value operational settings (e.g. LastPaymentReminderSentDate).
    // Renamed from "SocietySetting" to avoid confusion with the Society tenant entity.
    public class AppSetting : ITenantScoped
    {
        public int AppSettingId { get; set; }
        public int SocietyId { get; set; }
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
        public string? Description { get; set; }
    }
}
