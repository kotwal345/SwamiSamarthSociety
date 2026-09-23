using SwamiSamarthSociety.Data;

namespace SwamiSamarthSociety.Web.Identity
{
    public class CurrentSocietyContext : ICurrentSocietyContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private bool _explicitlySet;
        private int? _explicitSocietyId;
        private bool _explicitIsSuperAdmin;

        public CurrentSocietyContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? SocietyId => _explicitlySet ? _explicitSocietyId : ResolveFromClaims().SocietyId;
        public bool IsSuperAdmin => _explicitlySet ? _explicitIsSuperAdmin : ResolveFromClaims().IsSuperAdmin;

        // Used by background workers (no HttpContext) to pin this DI scope's DbContext to one
        // specific society before resolving it -- see MonthlyReminderBackgroundService.
        public void SetExplicit(int? societyId, bool isSuperAdmin)
        {
            _explicitlySet = true;
            _explicitSocietyId = societyId;
            _explicitIsSuperAdmin = isSuperAdmin;
        }

        private (int? SocietyId, bool IsSuperAdmin) ResolveFromClaims()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true) return (null, false);

            var isSuperAdmin = user.IsInRole(AppRoles.SuperAdmin);
            var claim = user.FindFirst(AppUserClaimsPrincipalFactory.SocietyIdClaimType)?.Value;
            var societyId = int.TryParse(claim, out var id) ? id : (int?)null;
            return (societyId, isSuperAdmin);
        }
    }
}
