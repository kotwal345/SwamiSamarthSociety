using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwamiSamarthSociety.Data;
using SwamiSamarthSociety.Services;

namespace SwamiSamarthSociety.Web.Controllers
{
    // The whole page (not just sending) is Chairman-only: the log shows every member's phone
    // number and message text, which regular members shouldn't be able to browse.
    [Authorize(Roles = AppRoles.Chairman)]
    public class RemindersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IPaymentReminderService _reminderService;
        private readonly IConfiguration _config;

        public RemindersController(ApplicationDbContext db, IPaymentReminderService reminderService, IConfiguration config)
        {
            _db = db;
            _reminderService = reminderService;
            _config = config;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.IsConfigured = !string.IsNullOrWhiteSpace(_config["Msg91:AuthKey"])
                && !string.IsNullOrWhiteSpace(_config["Msg91:TemplateId"]);

            var membersWithoutMobile = await _db.Members
                .Where(m => m.Status == "Active" && string.IsNullOrEmpty(m.MobileNumber))
                .CountAsync();
            ViewBag.MembersWithoutMobile = membersWithoutMobile;

            var logs = await _db.SmsLogs
                .Include(l => l.Member)
                .OrderByDescending(l => l.SentDate)
                .Take(100)
                .ToListAsync();
            return View(logs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendNow()
        {
            var result = await _reminderService.SendMonthlyRemindersAsync();
            TempData["Success"] = $"Reminder batch sent: {result.Sent} sent, {result.Failed} failed, " +
                                   $"{result.SkippedNoMobile} skipped (no mobile number on file), out of {result.TotalActiveMembers} active members.";
            return RedirectToAction(nameof(Index));
        }
    }
}
