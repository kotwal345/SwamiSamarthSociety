using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwamiSamarthSociety.Data;
using SwamiSamarthSociety.Data.Entities;
using SwamiSamarthSociety.Web.Areas.SuperAdmin.Models;
using SwamiSamarthSociety.Web.Identity;

namespace SwamiSamarthSociety.Web.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Roles = AppRoles.SuperAdmin)]
    public class SocietiesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public SocietiesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // SuperAdmin has no SocietyId, so every query here deliberately ignores the tenant
        // filter -- that's the whole point of this area.
        public async Task<IActionResult> Index()
        {
            var societies = await _db.Societies.IgnoreQueryFilters().OrderBy(s => s.Name).ToListAsync();

            var vms = new List<SocietyListItemViewModel>();
            foreach (var s in societies)
            {
                var memberCount = await _db.Members.IgnoreQueryFilters().CountAsync(m => m.SocietyId == s.SocietyId);
                vms.Add(new SocietyListItemViewModel
                {
                    SocietyId = s.SocietyId,
                    Name = s.Name,
                    Code = s.Code,
                    IsActive = s.IsActive,
                    CreatedDate = s.CreatedDate,
                    MemberCount = memberCount
                });
            }
            return View(vms);
        }

        public IActionResult Create() => View(new CreateSocietyViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateSocietyViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var normalizedCode = vm.Code.Trim().ToUpperInvariant();
            if (await _db.Societies.IgnoreQueryFilters().AnyAsync(s => s.Code == normalizedCode))
            {
                ModelState.AddModelError(nameof(vm.Code), "This code is already in use by another society.");
                return View(vm);
            }

            var normalizedEmail = vm.AdminEmail.Trim().ToUpperInvariant();
            if (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.NormalizedEmail == normalizedEmail))
            {
                ModelState.AddModelError(nameof(vm.AdminEmail), "An account with this email already exists.");
                return View(vm);
            }

            var society = new Society
            {
                Name = vm.Name.Trim(),
                NameMarathi = string.IsNullOrWhiteSpace(vm.NameMarathi) ? null : vm.NameMarathi.Trim(),
                Code = normalizedCode,
                ContactPersonName = string.IsNullOrWhiteSpace(vm.ContactPersonName) ? null : vm.ContactPersonName.Trim(),
                ContactPhone = string.IsNullOrWhiteSpace(vm.ContactPhone) ? null : vm.ContactPhone.Trim(),
                ContactEmail = string.IsNullOrWhiteSpace(vm.ContactEmail) ? null : vm.ContactEmail.Trim(),
                Address = string.IsNullOrWhiteSpace(vm.Address) ? null : vm.Address.Trim(),
                IsActive = true,
                CreatedBy = User.Identity?.Name
            };
            _db.Societies.Add(society);
            await _db.SaveChangesAsync();

            var tempPassword = TempPasswordGenerator.Generate();
            var admin = new ApplicationUser
            {
                UserName = vm.AdminEmail.Trim(),
                Email = vm.AdminEmail.Trim(),
                EmailConfirmed = true,
                SocietyId = society.SocietyId,
                MustChangePassword = true
            };
            var result = await _userManager.CreateAsync(admin, tempPassword);
            if (!result.Succeeded)
            {
                _db.Societies.Remove(society);
                await _db.SaveChangesAsync();
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(vm);
            }
            await _userManager.AddToRoleAsync(admin, AppRoles.SocietyAdmin);

            TempData["CreatedSocietyName"] = society.Name;
            TempData["CreatedAdminEmail"] = admin.Email;
            TempData["CreatedTempPassword"] = tempPassword;
            return RedirectToAction(nameof(Created));
        }

        // Shown exactly once, right after Create -- TempData is cleared as soon as it's read,
        // so refreshing or navigating away and back no longer shows the password.
        public new IActionResult Created()
        {
            if (TempData["CreatedAdminEmail"] is not string email) return RedirectToAction(nameof(Index));

            var vm = new SocietyCreatedViewModel
            {
                SocietyName = TempData["CreatedSocietyName"] as string ?? "",
                AdminEmail = email,
                TempPassword = TempData["CreatedTempPassword"] as string ?? ""
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var society = await _db.Societies.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.SocietyId == id);
            if (society is null) return NotFound();

            society.IsActive = false;
            society.DeactivatedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"{society.Name} has been deactivated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
        {
            var society = await _db.Societies.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.SocietyId == id);
            if (society is null) return NotFound();

            society.IsActive = true;
            society.DeactivatedDate = null;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"{society.Name} has been reactivated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
