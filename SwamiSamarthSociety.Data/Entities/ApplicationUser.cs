using Microsoft.AspNetCore.Identity;

namespace SwamiSamarthSociety.Data.Entities
{
    // Extends the stock Identity user with a tenant link.
    // SocietyId is null only for SuperAdmin (the product owner); every SocietyAdmin
    // and Member belongs to exactly one Society.
    public class ApplicationUser : IdentityUser
    {
        public int? SocietyId { get; set; }
        public Society? Society { get; set; }

        // True right after an admin (SuperAdmin or SocietyAdmin) provisions this account
        // with a generated temp password; forces a password change before anything else.
        public bool MustChangePassword { get; set; } = false;
    }
}
