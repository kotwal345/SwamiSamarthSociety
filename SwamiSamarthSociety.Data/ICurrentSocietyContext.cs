namespace SwamiSamarthSociety.Data
{
    // Resolves which Society the current unit of work is scoped to.
    // In a web request this reads the "SocietyId" claim baked into the signed-in user's
    // auth cookie at login (see AppUserClaimsPrincipalFactory in the Web project).
    // A background worker has no HttpContext/claims to read, so it explicitly sets the
    // tenant for its own DI scope via SetExplicit before resolving the DbContext/services
    // it needs -- see MonthlyReminderBackgroundService.
    public interface ICurrentSocietyContext
    {
        int? SocietyId { get; }
        bool IsSuperAdmin { get; }

        void SetExplicit(int? societyId, bool isSuperAdmin);
    }
}
