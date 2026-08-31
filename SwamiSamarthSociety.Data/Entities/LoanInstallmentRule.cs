using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwamiSamarthSociety.Data.Entities
{
    public class LoanInstallmentRule
    {
        public int LoanInstallmentRuleId { get; set; }
        public decimal MinimumAmount { get; set; }
        public decimal MaximumAmount { get; set; }
        public decimal? InstallmentPercentage { get; set; }
        public int? NumberOfInstallments { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class SocietySetting
    {
        public int SocietySettingId { get; set; }
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
        public string? Description { get; set; }
    }
}
