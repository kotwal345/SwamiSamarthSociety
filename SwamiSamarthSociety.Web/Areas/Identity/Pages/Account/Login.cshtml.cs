#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SwamiSamarthSociety.Data;
using SwamiSamarthSociety.Data.Entities;

namespace SwamiSamarthSociety.Web.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _db = db;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Enter a valid email address.")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Password is required.")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me")]
            public bool RememberMe { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ReturnUrl = returnUrl;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
            {
                ReturnUrl = returnUrl;
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(
                Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // The query filter scopes _userManager/Users to the caller's own tenant, which
                // isn't populated yet within this same request (the auth cookie was just issued
                // for the *response*, not applied to HttpContext.User for the rest of this
                // request) -- so look the user up directly, ignoring the filter, instead.
                var normalizedEmail = Input.Email.ToUpperInvariant();
                var user = await _db.Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

                if (user?.SocietyId is { } societyId)
                {
                    var society = await _db.Societies.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(s => s.SocietyId == societyId);
                    if (society is { IsActive: false })
                    {
                        await _signInManager.SignOutAsync();
                        ModelState.AddModelError(string.Empty, "This society's account has been deactivated. Contact the site administrator.");
                        ReturnUrl = returnUrl;
                        return Page();
                    }
                }

                if (user is { MustChangePassword: true })
                {
                    return RedirectToPage("./ChangePassword", new { forced = true });
                }
                return LocalRedirect(returnUrl);
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "This account has been locked out after too many failed attempts. Please try again later.");
                ReturnUrl = returnUrl;
                return Page();
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            ReturnUrl = returnUrl;
            return Page();
        }
    }
}
