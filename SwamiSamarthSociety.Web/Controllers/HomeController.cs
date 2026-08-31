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
        public HomeController(IMemberService memberService) => _memberService = memberService;

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var members = await _memberService.GetAllAsync(includeInactive: true);
            var vm = new DashboardViewModel
            {
                TotalMembers = members.Count,
                ActiveMembers = members.Count(m => m.Status == "Active"),
                InactiveMembers = members.Count(m => m.Status != "Active"),
                RecentMembers = members.OrderByDescending(m => m.JoiningDate).Take(5).ToList()
            };
            return View(vm);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
