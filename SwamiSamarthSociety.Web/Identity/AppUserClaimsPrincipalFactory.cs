using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using SwamiSamarthSociety.Data.Entities;

namespace SwamiSamarthSociety.Web.Identity
{
    // Bakes the signed-in user's SocietyId into their auth cookie at login, so tenant
    // resolution (CurrentSocietyContext) needs no DB lookup per request. SuperAdmin
    // accounts have SocietyId == null and simply get no claim.
    public class AppUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        public const string SocietyIdClaimType = "SocietyId";
        public const string MustChangePasswordClaimType = "MustChangePassword";

        public AppUserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            Microsoft.Extensions.Options.IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor)
        {
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            if (user.SocietyId is { } societyId)
                identity.AddClaim(new Claim(SocietyIdClaimType, societyId.ToString()));
            if (user.MustChangePassword)
                identity.AddClaim(new Claim(MustChangePasswordClaimType, "true"));
            return identity;
        }
    }
}
