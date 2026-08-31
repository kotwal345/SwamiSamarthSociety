using SwamiSamarthSociety.Data.Entities;
using SwamiSamarthSociety.Services;

namespace SwamiSamarthSociety.Web.Models
{
    public class MonthlyCycleDetailsViewModel
    {
        public MonthlyCycle Cycle { get; set; } = null!;
        public List<MonthlyCycleRow> Rows { get; set; } = new();
    }
}
