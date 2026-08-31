using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SwamiSamarthSociety.Services;
using SwamiSamarthSociety.Data.Entities;

namespace SwamiSamarthSociety.Web.Controllers
{
    [Authorize]
    public class MembersController : Controller
    {
        private readonly IMemberService _memberService;
        public MembersController(IMemberService memberService) => _memberService = memberService;

        public async Task<IActionResult> Index()
        {
            var members = await _memberService.GetAllAsync(includeInactive: true);
            return View(members);
        }

        public async Task<IActionResult> Details(int id)
        {
            var member = await _memberService.GetByIdAsync(id);
            if (member is null) return NotFound();
            ViewBag.CumulativeShare = await _memberService.GetCumulativeShareAsync(id);
            return View(member);
        }

        public IActionResult Create() => View(new Member { JoiningDate = DateTime.Today });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Member member)
        {
            // MemberCode is generated server-side by MemberService.CreateAsync, not submitted by the form.
            ModelState.Remove(nameof(Member.MemberCode));
            if (!ModelState.IsValid) return View(member);
            await _memberService.CreateAsync(member);
            TempData["Success"] = $"Member {member.FullName} added as {member.MemberCode}.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var member = await _memberService.GetByIdAsync(id);
            if (member is null) return NotFound();
            return View(member);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Member member)
        {
            if (id != member.MemberId) return BadRequest();

            // The Edit form only carries a subset of fields; MemberCode isn't one of them.
            ModelState.Remove(nameof(Member.MemberCode));
            if (!ModelState.IsValid) return View(member);

            var existing = await _memberService.GetByIdAsync(id);
            if (existing is null) return NotFound();

            // Apply only the fields the Edit form actually exposes, so fields it omits
            // (JoiningDate, MonthlyShareAmount, MemberCode, ...) aren't clobbered.
            existing.FullName = member.FullName;
            existing.MobileNumber = member.MobileNumber;
            existing.Address = member.Address;
            existing.Status = member.Status;

            await _memberService.UpdateAsync(existing);
            TempData["Success"] = "Member updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
