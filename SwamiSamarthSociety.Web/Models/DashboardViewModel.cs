using SwamiSamarthSociety.Data.Entities;

namespace SwamiSamarthSociety.Web.Models
{
    public class DashboardViewModel
    {
        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }
        public int InactiveMembers { get; set; }
        public List<Member> RecentMembers { get; set; } = new();
    }
}
