using SwamiSamarthSociety.Data.Entities;

namespace SwamiSamarthSociety.Web.Models
{
    public class DashboardViewModel
    {
        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }
        public int InactiveMembers { get; set; }
        public List<Member> RecentMembers { get; set; } = new();

        // All-time financial history, summed across every monthly cycle ever run.
        public decimal TotalShareCollected { get; set; }
        public decimal TotalPrincipalCollected { get; set; }
        public decimal TotalInterestCollected { get; set; }
        public decimal TotalCollectedAllTime => TotalShareCollected + TotalInterestCollected;

        public int MonthsTracked { get; set; }
        public int? FirstCycleMonth { get; set; }
        public int? FirstCycleYear { get; set; }
        public int? LatestCycleMonth { get; set; }
        public int? LatestCycleYear { get; set; }

        public decimal CurrentBankBalance { get; set; }

        public int ActiveLoansCount { get; set; }
        public decimal TotalOutstandingPrincipal { get; set; }

        // Oldest -> newest, one point per monthly cycle, for the collections-over-time chart.
        public List<MonthlyHistoryPoint> MonthlyHistory { get; set; } = new();
    }

    public class MonthlyHistoryPoint
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal ShareCollected { get; set; }
        public decimal PrincipalCollected { get; set; }
        public decimal InterestCollected { get; set; }
        public decimal Total => ShareCollected + PrincipalCollected + InterestCollected;
    }
}
