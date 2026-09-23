#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SwamiSamarthSociety.Data.Entities;

namespace SwamiSamarthSociety.Web.Areas.Identity.Pages.Account
{
    [Authorize]
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<ChangePasswordModel> _logger;

        public ChangePasswordModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<ChangePasswordModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public bool Forced { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Current password is required.")]
            [DataType(DataType.Password)]
            [Display(Name = "Current password")]
            public string OldPassword { get; set; }

            [Required(ErrorMessage = "New password is required.")]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(bool forced = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();

            Forced = forced || user.MustChangePassword;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(bool forced = false)
        {
            Forced = forced;
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ChangePassword: model invalid -- {Errors}",
                    string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                _logger.LogWarning("ChangePassword: GetUserAsync(User) returned null for signed-in principal {Name}.", User.Identity?.Name);
                return NotFound();
            }

            var result = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
            if (!result.Succeeded)
            {
                _logger.LogWarning("ChangePassword: ChangePasswordAsync failed for {Email} -- {Errors}",
                    user.Email, string.Join("; ", result.Errors.Select(e => e.Description)));
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return Page();
            }

            if (user.MustChangePassword)
            {
                user.MustChangePassword = false;
                await _userManager.UpdateAsync(user);
            }

            // Re-issues the auth cookie with fresh claims (including the now-cleared
            // MustChangePassword state) so RequirePasswordChangeMiddleware stops redirecting
            // here without requiring the user to log out and back in.
            await _signInManager.RefreshSignInAsync(user);

            _logger.LogInformation("ChangePassword: succeeded for {Email}.", user.Email);
            StatusMessage = "Your password has been changed.";
            return RedirectToPage();
        }
    }
}
