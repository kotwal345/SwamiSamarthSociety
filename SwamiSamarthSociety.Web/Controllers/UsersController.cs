using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwamiSamarthSociety.Data;
using SwamiSamarthSociety.Data.Entities;
using SwamiSamarthSociety.Services;
using SwamiSamarthSociety.Web.Identity;
using SwamiSamarthSociety.Web.Models;

namespace SwamiSamarthSociety.Web.Controllers
{
    // Lets a SocietyAdmin provision Member logins within their own society. ApplicationUser has
    // no ambient tenant query filter (see ApplicationDbContext.OnModelCreating), so Index()
    // filters _db.Users by the caller's own SocietyId explicitly -- this controller never needs
    // to (and never should) accept a SocietyId from the caller.
    [Authorize(Roles = AppRoles.SocietyAdmin)]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentSocietyContext _tenant;

        public UsersController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ICurrentSocietyContext tenant)
        {
            _db = db;
            _userManager = userManager;
            _tenant = tenant;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _db.Users.Where(u => u.SocietyId == _tenant.SocietyId).OrderBy(u => u.Email).ToListAsync();
            var vms = new List<UserListItemViewModel>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                vms.Add(new UserListItemViewModel
                {
                    Id = u.Id,
                    Email = u.Email ?? "",
                    Role = roles.FirstOrDefault() ?? "(none)",
                    MustChangePassword = u.MustChangePassword
                });
            }
            return View(vms);
        }

        public IActionResult Create() => View(new CreateMemberLoginViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMemberLoginViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var normalizedEmail = vm.Email.Trim().ToUpperInvariant();
            if (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.NormalizedEmail == normalizedEmail))
            {
                ModelState.AddModelError(nameof(vm.Email), "An account with this email already exists.");
                return View(vm);
            }

            var tempPassword = TempPasswordGenerator.Generate();
            var member = new ApplicationUser
            {
                UserName = vm.Email.Trim(),
                Email = vm.Email.Trim(),
                EmailConfirmed = true,
                SocietyId = _tenant.SocietyId,
                MustChangePassword = true
            };
            var result = await _userManager.CreateAsync(member, tempPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(vm);
            }
            await _userManager.AddToRoleAsync(member, AppRoles.Member);

            TempData["CreatedEmail"] = member.Email;
            TempData["CreatedTempPassword"] = tempPassword;
            return RedirectToAction(nameof(Created));
        }

        public new IActionResult Created()
        {
            if (TempData["CreatedEmail"] is not string email) return RedirectToAction(nameof(Index));

            var vm = new MemberLoginCreatedViewModel
            {
                Email = email,
                TempPassword = TempData["CreatedTempPassword"] as string ?? ""
            };
            return View(vm);
        }
    }
}
