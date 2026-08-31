using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SwamiSamarthSociety.Services;
using SwamiSamarthSociety.Web.Models;

namespace SwamiSamarthSociety.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMemberService _memberService;
        private readonly IMonthlyCycleService _monthlyCycleService;
        private readonly ILoanService _loanService;

        public HomeController(IMemberService memberService, IMonthlyCycleService monthlyCycleService, ILoanService loanService)
        {
            _memberService = memberService;
            _monthlyCycleService = monthlyCycleService;
            _loanService = loanService;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var members = await _memberService.GetAllAsync(includeInactive: true);
            var cycles = await _monthlyCycleService.GetAllAsync(); // newest first
            var loans = await _loanService.GetAllAsync();

            var oldestCycle = cycles.OrderBy(c => c.Year).ThenBy(c => c.Month).FirstOrDefault();
            var latestCycle = cycles.FirstOrDefault();

            var vm = new DashboardViewModel
            {
                TotalMembers = members.Count,
                ActiveMembers = members.Count(m => m.Status == "Active"),
                InactiveMembers = members.Count(m => m.Status != "Active"),
                RecentMembers = members.OrderByDescending(m => m.JoiningDate).Take(5).ToList(),

                TotalShareCollected = cycles.Sum(c => c.CollectedShareAmount),
                TotalPrincipalCollected = cycles.Sum(c => c.TotalPrincipalCollected),
                TotalInterestCollected = cycles.Sum(c => c.TotalInterestCollected),

                MonthsTracked = cycles.Count,
                FirstCycleMonth = oldestCycle?.Month,
                FirstCycleYear = oldestCycle?.Year,
                LatestCycleMonth = latestCycle?.Month,
                LatestCycleYear = latestCycle?.Year,

                CurrentBankBalance = latestCycle is null ? 0m : latestCycle.ClosingBankBalance ?? latestCycle.OpeningBankBalance,

                ActiveLoansCount = loans.Count(l => l.Status == "Active"),
                TotalOutstandingPrincipal = loans.Where(l => l.Status == "Active").Sum(l => l.OutstandingPrincipal),

                MonthlyHistory = cycles
                    .OrderBy(c => c.Year).ThenBy(c => c.Month)
                    .Select(c => new MonthlyHistoryPoint
                    {
                        Year = c.Year,
                        Month = c.Month,
                        ShareCollected = c.CollectedShareAmount,
                        PrincipalCollected = c.TotalPrincipalCollected,
                        InterestCollected = c.TotalInterestCollected
                    })
                    .ToList()
            };
            return View(vm);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
